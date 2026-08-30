import { api } from "./api.js";
import { money, dateInputValue, toast, confirmar } from "./ui.js";
import { openModal, closeModal } from "./modal.js";

let deudas = [];
let periodos = [];
let categorias = [];

export async function render(container) {
  [deudas, periodos, categorias] = await Promise.all([
    api.get("/deudas"),
    api.get("/periodos"),
    api.get("/categorias"),
  ]);

  container.innerHTML = vista();
  bind(container);
}

function totales() {
  const activas = deudas.filter(d => d.estado === "ACTIVA");
  const pendiente = activas.reduce((s, d) => s + d.saldoPendiente, 0);
  const pagado = deudas.reduce((s, d) => s + d.montoPagado, 0);
  return { activas: activas.length, pendiente, pagado };
}

function vista() {
  const t = totales();
  return `
    <div class="d-flex justify-content-between align-items-center mb-3">
      <h1 class="h4 mb-0">💳 Deudas</h1>
      <button class="btn btn-primary btn-sm" id="btn-nueva-deuda">+ Nueva deuda</button>
    </div>

    <div class="row g-3 mb-4">
      <div class="col-md-4"><div class="card tile bg-info-subtle"><div class="card-body">
        <div class="text-muted small">Activas</div><div class="h4 mb-0">${t.activas}</div></div></div></div>
      <div class="col-md-4"><div class="card tile bg-danger-subtle"><div class="card-body">
        <div class="text-muted small">Pendiente</div><div class="h4 mb-0">${money(t.pendiente)}</div></div></div></div>
      <div class="col-md-4"><div class="card tile bg-success-subtle"><div class="card-body">
        <div class="text-muted small">Pagado</div><div class="h4 mb-0">${money(t.pagado)}</div></div></div></div>
    </div>

    <div class="row g-3">
      ${deudas.map(deudaCardHtml).join("") || `<div class="text-muted">Sin deudas registradas.</div>`}
    </div>`;
}

function deudaCardHtml(d) {
  const pagada = d.estado === "PAGADA";
  return `
    <div class="col-md-6">
      <div class="card">
        <div class="card-body">
          <div class="d-flex justify-content-between">
            <strong>${d.nombre}</strong>
            <span class="badge ${pagada ? "text-bg-success" : "text-bg-warning"}">${pagada ? "Pagada" : "Activa"}</span>
          </div>
          <div class="text-muted small mb-2">${d.acreedor}</div>
          <div class="progress mb-2"><div class="progress-bar bg-success" style="width:${Math.min(d.porcentajePagado, 100)}%"></div></div>
          <div class="d-flex justify-content-between small mb-2">
            <span>Pagado: ${money(d.montoPagado)}</span>
            <span>Pendiente: ${money(d.saldoPendiente)}</span>
          </div>
          <div class="btn-group btn-group-sm w-100" data-id="${d.id}">
            <button class="btn btn-outline-primary btn-abonar" ${pagada ? "disabled" : ""}>Abonar</button>
            <button class="btn btn-outline-secondary btn-historial">Historial</button>
            <button class="btn btn-outline-danger btn-eliminar">Eliminar</button>
          </div>
        </div>
      </div>
    </div>`;
}

function bind(container) {
  document.getElementById("btn-nueva-deuda").onclick = () => formNuevaDeuda(container);

  for (const grp of document.querySelectorAll("[data-id]")) {
    const id = Number(grp.dataset.id);
    const d = deudas.find(x => x.id === id);
    grp.querySelector(".btn-abonar")?.addEventListener("click", () => formAbono(container, d));
    grp.querySelector(".btn-historial")?.addEventListener("click", () => verHistorial(d));
    grp.querySelector(".btn-eliminar")?.addEventListener("click", () => eliminar(container, id));
  }
}

function formNuevaDeuda(container) {
  const body = openModal("Nueva deuda", `
    <div class="mb-3"><label class="form-label">Nombre</label><input type="text" class="form-control" id="f-nombre"></div>
    <div class="mb-3"><label class="form-label">Acreedor</label><input type="text" class="form-control" id="f-acreedor"></div>
    <div class="mb-3"><label class="form-label">Monto</label><input type="number" step="0.01" min="0.01" class="form-control" id="f-monto"></div>
    <div class="mb-3"><label class="form-label">Vencimiento (opcional)</label><input type="date" class="form-control" id="f-venc"></div>
    <div class="mb-3"><label class="form-label">Descripción</label><input type="text" class="form-control" id="f-desc"></div>
    <button class="btn btn-primary w-100" id="f-guardar">Guardar</button>`);

  body.querySelector("#f-guardar").onclick = async () => {
    const payload = {
      nombre: body.querySelector("#f-nombre").value,
      acreedor: body.querySelector("#f-acreedor").value,
      montoOriginal: Number(body.querySelector("#f-monto").value),
      fechaVencimiento: body.querySelector("#f-venc").value || null,
      fechaInicio: dateInputValue(),
      descripcion: body.querySelector("#f-desc").value,
    };
    try {
      await api.post("/deudas", payload);
      closeModal();
      await render(container);
      toast("Deuda creada.", "success");
    } catch (e) { toast(e.message, "danger"); }
  };
}

function formAbono(container, d) {
  const periodoActual = periodos[0];
  const body = openModal(`Abonar a "${d.nombre}"`, `
    <p class="text-muted">Saldo pendiente: ${money(d.saldoPendiente)}</p>
    <div class="mb-3"><label class="form-label">Monto</label>
      <input type="number" step="0.01" min="0.01" max="${d.saldoPendiente}" class="form-control" id="f-monto"></div>
    <div class="mb-3"><label class="form-label">Nota (opcional)</label><input type="text" class="form-control" id="f-desc"></div>
    <button class="btn btn-primary w-100" id="f-guardar">Registrar abono</button>`);

  body.querySelector("#f-guardar").onclick = async () => {
    if (!periodoActual) { toast("No hay un periodo activo para registrar el abono.", "danger"); return; }
    const payload = {
      periodoId: periodoActual.id,
      categoriaId: categorias.find(c => c.nombre.toLowerCase().includes("deuda"))?.id ?? null,
      monto: Number(body.querySelector("#f-monto").value),
      descripcion: body.querySelector("#f-desc").value,
    };
    try {
      await api.post(`/deudas/${d.id}/abonos`, payload);
      closeModal();
      await render(container);
      toast("Abono registrado.", "success");
    } catch (e) { toast(e.message, "danger"); }
  };
}

async function verHistorial(d) {
  const abonos = await api.get(`/deudas/${d.id}/abonos`);
  openModal(`Historial de "${d.nombre}"`, `
    <table class="table table-sm">
      <thead><tr><th>Fecha</th><th>Descripción</th><th class="text-end">Monto</th></tr></thead>
      <tbody>
        ${abonos.map(a => `<tr><td>${a.fecha.slice(0, 10)}</td><td>${a.descripcion}</td><td class="text-end">${money(a.monto)}</td></tr>`).join("")
          || `<tr><td colspan="3" class="text-muted text-center py-3">Sin abonos</td></tr>`}
      </tbody>
    </table>`);
}

async function eliminar(container, id) {
  if (!confirmar("¿Eliminar esta deuda?")) return;
  try {
    await api.del(`/deudas/${id}`);
    await render(container);
    toast("Eliminada.", "success");
  } catch (e) { toast(e.message, "danger"); }
}
