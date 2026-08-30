import { api } from "./api.js";
import { money, dateInputValue, fechaCorta, esc, toast, confirmar } from "./ui.js";
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
  return {
    activas: activas.length,
    pendiente: activas.reduce((s, d) => s + d.saldoPendiente, 0),
    pagado: deudas.reduce((s, d) => s + d.montoPagado, 0),
  };
}

function vista() {
  const t = totales();
  return `
    <div class="d-flex align-items-center justify-content-between mb-3">
      <h1 class="h5 fw-semibold mb-0">Deudas</h1>
      <button class="btn btn-primary btn-sm" id="btn-nueva-deuda"><i class="bi bi-plus-lg me-1"></i>Nueva deuda</button>
    </div>

    <div class="row g-3 mb-3">
      <div class="col-sm-4"><div class="surface tile">
        <div class="label">Activas</div><div class="tile-value numeric">${t.activas}</div></div></div>
      <div class="col-sm-4"><div class="surface tile">
        <div class="label">Pendiente</div><div class="tile-value numeric text-neg">${money(t.pendiente)}</div></div></div>
      <div class="col-sm-4"><div class="surface tile">
        <div class="label">Pagado</div><div class="tile-value numeric text-pos">${money(t.pagado)}</div></div></div>
    </div>

    ${deudas.length
      ? `<div class="row g-3">${deudas.map(deudaCard).join("")}</div>`
      : `<div class="surface empty-state"><i class="bi bi-credit-card d-block fs-4 mb-2"></i>Sin deudas registradas.</div>`}`;
}

function deudaCard(d) {
  const pagada = d.estado === "PAGADA";
  return `
    <div class="col-md-6">
      <div class="surface p-3 h-100 d-flex flex-column" data-did="${d.id}">
        <div class="d-flex justify-content-between align-items-start gap-2">
          <div class="min-w-0">
            <div class="fw-medium text-truncate">${esc(d.nombre)}</div>
            <div class="text-muted-app" style="font-size:.75rem">
              <i class="bi bi-person me-1"></i>${esc(d.acreedor)}
              ${d.fechaVencimiento ? ` · vence ${fechaCorta(d.fechaVencimiento)}` : ""}
            </div>
          </div>
          <span class="chip ${pagada ? "chip-OK" : "chip-CRITICO"}">${pagada ? "Pagada" : "Activa"}</span>
        </div>

        <div class="progress my-3"><div class="progress-bar ${pagada ? "estado-OK" : "estado-CRITICO"}" style="width:${Math.min(d.porcentajePagado, 100)}%"></div></div>

        <div class="d-flex justify-content-between numeric mb-3" style="font-size:.8125rem">
          <span class="text-muted-app">Pagado ${money(d.montoPagado)} de ${money(d.montoOriginal)}</span>
          <span class="${pagada ? "text-pos" : "text-neg"}">${pagada ? "Saldada" : money(d.saldoPendiente)}</span>
        </div>

        <div class="d-flex gap-2 mt-auto">
          <button class="btn btn-primary btn-sm flex-grow-1 btn-abonar" ${pagada ? "disabled" : ""}>
            <i class="bi bi-cash-coin me-1"></i>Abonar
          </button>
          <button class="btn btn-quiet btn-sm btn-historial" title="Historial"><i class="bi bi-clock-history"></i></button>
          <button class="btn btn-quiet btn-sm btn-eliminar" title="Eliminar"><i class="bi bi-trash"></i></button>
        </div>
      </div>
    </div>`;
}

function bind(container) {
  container.querySelector("#btn-nueva-deuda").onclick = () => formNuevaDeuda(container);

  for (const card of container.querySelectorAll("[data-did]")) {
    const d = deudas.find(x => x.id === Number(card.dataset.did));
    card.querySelector(".btn-abonar").onclick = () => formAbono(container, d);
    card.querySelector(".btn-historial").onclick = () => verHistorial(d);
    card.querySelector(".btn-eliminar").onclick = () => eliminar(container, d.id);
  }
}

function formNuevaDeuda(container) {
  const body = openModal("Nueva deuda", `
    <div class="row g-3">
      <div class="col-12"><label class="form-label" for="f-nombre">Nombre</label>
        <input type="text" class="form-control" id="f-nombre" autofocus></div>
      <div class="col-12"><label class="form-label" for="f-acreedor">Acreedor</label>
        <input type="text" class="form-control" id="f-acreedor"></div>
      <div class="col-6"><label class="form-label" for="f-monto">Monto</label>
        <input type="number" step="0.01" min="0.01" class="form-control" id="f-monto"></div>
      <div class="col-6"><label class="form-label" for="f-venc">Vencimiento</label>
        <input type="date" class="form-control" id="f-venc"></div>
      <div class="col-12"><label class="form-label" for="f-desc">Descripción</label>
        <input type="text" class="form-control" id="f-desc"></div>
      <div class="col-12"><button class="btn btn-primary w-100" id="f-guardar">Guardar</button></div>
    </div>`);

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
      toast("Deuda creada.");
    } catch (e) { toast(e.message, "danger"); }
  };
}

function formAbono(container, d) {
  const periodoActual = periodos[0];
  const body = openModal(`Abonar a “${d.nombre}”`, `
    <div class="surface tile mb-3">
      <div class="label">Saldo pendiente</div>
      <div class="tile-value numeric text-neg">${money(d.saldoPendiente)}</div>
    </div>
    <div class="row g-3">
      <div class="col-12"><label class="form-label" for="f-monto">Monto del abono</label>
        <input type="number" step="0.01" min="0.01" max="${d.saldoPendiente}" class="form-control" id="f-monto" autofocus></div>
      <div class="col-12"><label class="form-label" for="f-desc">Nota</label>
        <input type="text" class="form-control" id="f-desc" placeholder="Opcional"></div>
      <div class="col-12"><button class="btn btn-primary w-100" id="f-guardar">Registrar abono</button></div>
    </div>`);

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
      toast("Abono registrado.");
    } catch (e) { toast(e.message, "danger"); }
  };
}

async function verHistorial(d) {
  const abonos = await api.get(`/deudas/${d.id}/abonos`);
  openModal(`Abonos de “${d.nombre}”`, `
    <table class="table table-hover">
      <thead><tr><th style="width:5rem">Fecha</th><th>Nota</th><th class="text-end">Monto</th></tr></thead>
      <tbody>
        ${abonos.map(a => `<tr>
            <td class="text-muted-app numeric" style="font-size:.8125rem">${fechaCorta(a.fecha)}</td>
            <td>${esc(a.descripcion) || "<span class='text-muted-app'>—</span>"}</td>
            <td class="text-end numeric fw-medium">${money(a.monto)}</td>
          </tr>`).join("") || `<tr><td colspan="3" class="empty-state">Sin abonos todavía</td></tr>`}
      </tbody>
    </table>`);
}

async function eliminar(container, id) {
  if (!confirmar("¿Eliminar esta deuda? También se conservan los abonos ya registrados como gastos.")) return;
  try {
    await api.del(`/deudas/${id}`);
    await render(container);
    toast("Deuda eliminada.");
  } catch (e) { toast(e.message, "danger"); }
}
