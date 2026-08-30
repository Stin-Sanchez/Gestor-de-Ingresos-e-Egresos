import { api } from "./api.js";
import { money, dateInputValue, fechaCorta, esc, toast, confirmar } from "./ui.js";
import { openModal, closeModal } from "./modal.js";

let deudas = [];
let resumen = null;
let periodos = [];
let categorias = [];
let filtro = "TODAS"; // TODAS | DEBO | ME_DEBEN

// La direccion de la deuda cambia el vocabulario y el color de toda la vista.
const DIR = {
  DEBO: {
    etiqueta: "Debo", contraparte: "Acreedor", accion: "Abonar",
    icono: "bi-arrow-up-right", clase: "text-neg", chip: "chip-CRITICO", barra: "estado-CRITICO",
  },
  ME_DEBEN: {
    etiqueta: "Me deben", contraparte: "Deudor", accion: "Registrar cobro",
    icono: "bi-arrow-down-left", clase: "text-pos", chip: "chip-OK", barra: "estado-OK",
  },
};

export async function render(container) {
  [deudas, resumen, periodos, categorias] = await Promise.all([
    api.get("/deudas"),
    api.get("/deudas/resumen"),
    api.get("/periodos"),
    api.get("/categorias"),
  ]);

  container.innerHTML = vista();
  bind(container);
}

function visibles() {
  return filtro === "TODAS" ? deudas : deudas.filter(d => d.tipo === filtro);
}

function vista() {
  const lista = visibles();
  return `
    <div class="d-flex align-items-center justify-content-between gap-2 mb-3 flex-wrap">
      <h1 class="h5 fw-semibold mb-0">Deudas</h1>
      <button class="btn btn-primary btn-sm" id="btn-nueva-deuda"><i class="bi bi-plus-lg me-1"></i>Nueva</button>
    </div>

    <div class="row g-3 mb-3">
      <div class="col-6 col-lg-3"><div class="surface tile">
        <div class="label">Debo</div>
        <div class="tile-value numeric text-neg">${money(resumen.debo)}</div>
        <div class="text-muted-app" style="font-size:.75rem">${resumen.activasDebo} activa${resumen.activasDebo === 1 ? "" : "s"}</div>
      </div></div>
      <div class="col-6 col-lg-3"><div class="surface tile">
        <div class="label">Me deben</div>
        <div class="tile-value numeric text-pos">${money(resumen.meDeben)}</div>
        <div class="text-muted-app" style="font-size:.75rem">${resumen.activasMeDeben} activa${resumen.activasMeDeben === 1 ? "" : "s"}</div>
      </div></div>
      <div class="col-12 col-lg-6"><div class="surface tile">
        <div class="label">Balance neto</div>
        <div class="tile-value numeric ${resumen.neto < 0 ? "text-neg" : "text-pos"}">${money(resumen.neto)}</div>
        <div class="text-muted-app" style="font-size:.75rem">
          ${resumen.neto < 0 ? "Debes más de lo que te deben" : resumen.neto > 0 ? "Te deben más de lo que debes" : "En equilibrio"}
        </div>
      </div></div>
    </div>

    <div class="seg mb-3" style="max-width:24rem">
      ${[["TODAS", "Todas"], ["DEBO", "Debo"], ["ME_DEBEN", "Me deben"]].map(([v, t]) =>
        `<button type="button" class="seg-btn ${filtro === v ? "active" : ""}" data-filtro="${v}">${t}</button>`).join("")}
    </div>

    ${lista.length
      ? `<div class="row g-3">${lista.map(deudaCard).join("")}</div>`
      : `<div class="surface empty-state"><i class="bi bi-credit-card d-block fs-4 mb-2"></i>Nada por aquí.</div>`}`;
}

function deudaCard(d) {
  const dir = DIR[d.tipo];
  const pagada = d.estado === "PAGADA";
  return `
    <div class="col-12 col-md-6 col-xl-4">
      <div class="surface p-3 h-100 d-flex flex-column" data-did="${d.id}">
        <div class="d-flex justify-content-between align-items-start gap-2">
          <div class="min-w-0">
            <div class="fw-medium text-truncate">
              <i class="bi ${dir.icono} ${dir.clase} me-1"></i>${esc(d.nombre)}
            </div>
            <div class="text-muted-app text-truncate" style="font-size:.75rem">
              ${dir.contraparte}: ${esc(d.acreedor)}
              ${d.fechaVencimiento ? ` · vence ${fechaCorta(d.fechaVencimiento)}` : ""}
            </div>
          </div>
          <span class="chip ${pagada ? "chip-OK" : dir.chip} flex-shrink-0">${pagada ? "Saldada" : dir.etiqueta}</span>
        </div>

        <div class="progress my-3"><div class="progress-bar ${pagada ? "estado-OK" : dir.barra}" style="width:${Math.min(d.porcentajePagado, 100)}%"></div></div>

        <div class="d-flex justify-content-between numeric mb-3 gap-2" style="font-size:.8125rem">
          <span class="text-muted-app text-truncate">${money(d.montoPagado)} de ${money(d.montoOriginal)}</span>
          <span class="${pagada ? "text-pos" : dir.clase} flex-shrink-0">${pagada ? "Completa" : money(d.saldoPendiente)}</span>
        </div>

        <div class="d-flex gap-2 mt-auto">
          <button class="btn btn-primary btn-sm flex-grow-1 btn-pagar" ${pagada ? "disabled" : ""}>
            <i class="bi bi-cash-coin me-1"></i>${dir.accion}
          </button>
          <button class="btn btn-quiet btn-sm btn-historial" title="Historial"><i class="bi bi-clock-history"></i></button>
          <button class="btn btn-quiet btn-sm btn-eliminar" title="Eliminar"><i class="bi bi-trash"></i></button>
        </div>
      </div>
    </div>`;
}

function bind(container) {
  container.querySelector("#btn-nueva-deuda").onclick = () => formNuevaDeuda(container);

  for (const btn of container.querySelectorAll("[data-filtro]")) {
    btn.onclick = () => { filtro = btn.dataset.filtro; render(container); };
  }

  for (const card of container.querySelectorAll("[data-did]")) {
    const d = deudas.find(x => x.id === Number(card.dataset.did));
    card.querySelector(".btn-pagar").onclick = () => formPago(container, d);
    card.querySelector(".btn-historial").onclick = () => verHistorial(d);
    card.querySelector(".btn-eliminar").onclick = () => eliminar(container, d);
  }
}

function formNuevaDeuda(container) {
  const body = openModal("Nueva deuda", `
    <div class="mb-3">
      <div class="seg">
        <button type="button" class="seg-btn active" data-tipo="DEBO">Yo debo</button>
        <button type="button" class="seg-btn" data-tipo="ME_DEBEN">Me deben</button>
      </div>
    </div>
    <div class="row g-3">
      <div class="col-12"><label class="form-label" for="f-nombre">Concepto</label>
        <input type="text" class="form-control" id="f-nombre" placeholder="Préstamo, tarjeta…" autofocus></div>
      <div class="col-12"><label class="form-label" for="f-acreedor"><span id="lbl-contraparte">Acreedor</span></label>
        <input type="text" class="form-control" id="f-acreedor" placeholder="Nombre de la persona o entidad"></div>
      <div class="col-12 col-sm-6"><label class="form-label" for="f-monto">Monto</label>
        <input type="number" step="0.01" min="0.01" class="form-control" id="f-monto"></div>
      <div class="col-12 col-sm-6"><label class="form-label" for="f-venc">Vencimiento</label>
        <input type="date" class="form-control" id="f-venc"></div>
      <div class="col-12"><label class="form-label" for="f-desc">Nota</label>
        <input type="text" class="form-control" id="f-desc" placeholder="Opcional"></div>
      <div class="col-12"><button class="btn btn-primary w-100" id="f-guardar">Guardar</button></div>
    </div>`);

  let tipo = "DEBO";
  for (const btn of body.querySelectorAll("[data-tipo]")) {
    btn.onclick = () => {
      tipo = btn.dataset.tipo;
      for (const b of body.querySelectorAll("[data-tipo]")) b.classList.toggle("active", b === btn);
      body.querySelector("#lbl-contraparte").textContent = DIR[tipo].contraparte;
    };
  }

  body.querySelector("#f-guardar").onclick = async () => {
    const payload = {
      tipo,
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

function formPago(container, d) {
  const dir = DIR[d.tipo];
  const periodoActual = periodos[0];
  const body = openModal(`${dir.accion} — ${d.nombre}`, `
    <div class="surface tile mb-3">
      <div class="label">Saldo pendiente</div>
      <div class="tile-value numeric ${dir.clase}">${money(d.saldoPendiente)}</div>
      <div class="text-muted-app" style="font-size:.75rem">
        ${d.tipo === "DEBO" ? "Se registrará como egreso del mes." : "Se registrará como ingreso del mes."}
      </div>
    </div>
    <div class="row g-3">
      <div class="col-12"><label class="form-label" for="f-monto">Monto</label>
        <input type="number" step="0.01" min="0.01" max="${d.saldoPendiente}" class="form-control" id="f-monto" autofocus></div>
      <div class="col-12"><label class="form-label" for="f-desc">Nota</label>
        <input type="text" class="form-control" id="f-desc" placeholder="Opcional"></div>
      <div class="col-12"><button class="btn btn-primary w-100" id="f-guardar">${dir.accion}</button></div>
    </div>`);

  body.querySelector("#f-guardar").onclick = async () => {
    if (!periodoActual) { toast("No hay un periodo activo para registrarlo.", "danger"); return; }
    const payload = {
      periodoId: periodoActual.id,
      categoriaId: categorias.find(c => c.nombre.toLowerCase().includes("deuda"))?.id ?? null,
      monto: Number(body.querySelector("#f-monto").value),
      descripcion: body.querySelector("#f-desc").value || `${dir.accion}: ${d.nombre}`,
    };
    try {
      await api.post(`/deudas/${d.id}/pagos`, payload);
      closeModal();
      await render(container);
      toast(d.tipo === "DEBO" ? "Abono registrado." : "Cobro registrado.");
    } catch (e) { toast(e.message, "danger"); }
  };
}

async function verHistorial(d) {
  const pagos = await api.get(`/deudas/${d.id}/pagos`);
  openModal(`Historial — ${d.nombre}`, `
    <div class="table-responsive">
      <table class="table table-hover">
        <thead><tr><th style="width:5rem">Fecha</th><th>Nota</th><th class="text-end">Monto</th></tr></thead>
        <tbody>
          ${pagos.map(p => `<tr>
              <td class="text-muted-app numeric" style="font-size:.8125rem">${fechaCorta(p.fecha)}</td>
              <td>${esc(p.descripcion) || "<span class='text-muted-app'>—</span>"}</td>
              <td class="text-end numeric fw-medium ${DIR[d.tipo].clase}">${money(p.monto)}</td>
            </tr>`).join("") || `<tr><td colspan="3" class="empty-state">Sin movimientos todavía</td></tr>`}
        </tbody>
      </table>
    </div>`);
}

async function eliminar(container, d) {
  if (!confirmar(`¿Eliminar "${d.nombre}"? Los movimientos ya registrados en los periodos se conservan.`)) return;
  try {
    await api.del(`/deudas/${d.id}`);
    await render(container);
    toast("Deuda eliminada.");
  } catch (e) { toast(e.message, "danger"); }
}
