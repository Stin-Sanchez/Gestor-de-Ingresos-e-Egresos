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

        <div class="d-flex gap-2 mt-auto flex-wrap">
          <button class="btn btn-primary btn-sm flex-grow-1 btn-pagar" ${pagada ? "disabled" : ""}>
            <i class="bi bi-cash-coin me-1"></i>${dir.accion}
          </button>
          <button class="btn btn-quiet btn-sm btn-ampliar" title="${d.tipo === "DEBO" ? "Me prestaron más" : "Presté más"}"><i class="bi bi-plus-circle"></i></button>
          <button class="btn btn-quiet btn-sm btn-editar" title="Editar"><i class="bi bi-pencil"></i></button>
          <button class="btn btn-quiet btn-sm btn-historial" title="Historial"><i class="bi bi-clock-history"></i></button>
          <button class="btn btn-quiet btn-sm btn-eliminar" title="Eliminar"><i class="bi bi-trash"></i></button>
        </div>
      </div>
    </div>`;
}

function bind(container) {
  container.querySelector("#btn-nueva-deuda").onclick = () => formDeuda(container);

  for (const btn of container.querySelectorAll("[data-filtro]")) {
    btn.onclick = () => { filtro = btn.dataset.filtro; render(container); };
  }

  for (const card of container.querySelectorAll("[data-did]")) {
    const d = deudas.find(x => x.id === Number(card.dataset.did));
    card.querySelector(".btn-pagar").onclick = () => formPago(container, d);
    card.querySelector(".btn-ampliar").onclick = () => formAmpliar(container, d);
    card.querySelector(".btn-editar").onclick = () => formDeuda(container, d);
    card.querySelector(".btn-historial").onclick = () => verHistorial(container, d);
    card.querySelector(".btn-eliminar").onclick = () => eliminar(container, d);
  }
}

// Mismo formulario para crear y para corregir. Al editar, la direccion queda fija: los
// pagos de una deuda que debo viven en gastos y los de una que me deben en ingresos, asi
// que darle la vuelta dejaria su historial en la tabla equivocada.
function formDeuda(container, d) {
  const edicion = Boolean(d);
  let tipo = d?.tipo ?? "DEBO";

  const body = openModal(edicion ? `Editar — ${d.nombre}` : "Nueva deuda", `
    ${edicion ? "" : `
    <div class="mb-3">
      <div class="seg">
        <button type="button" class="seg-btn active" data-tipo="DEBO">Yo debo</button>
        <button type="button" class="seg-btn" data-tipo="ME_DEBEN">Me deben</button>
      </div>
    </div>`}
    <div class="row g-3">
      <div class="col-12"><label class="form-label" for="f-nombre">Concepto</label>
        <input type="text" class="form-control" id="f-nombre" placeholder="Préstamo, tarjeta…" value="${esc(d?.nombre ?? "")}" autofocus></div>
      <div class="col-12"><label class="form-label" for="f-acreedor"><span id="lbl-contraparte">${DIR[tipo].contraparte}</span></label>
        <input type="text" class="form-control" id="f-acreedor" placeholder="Nombre de la persona o entidad" value="${esc(d?.acreedor ?? "")}"></div>
      <div class="col-12 col-sm-6"><label class="form-label" for="f-monto">Monto</label>
        <input type="number" step="0.01" min="0.01" class="form-control" id="f-monto" value="${d?.montoOriginal ?? ""}">
        ${edicion && d.montoPagado > 0
          ? `<div class="form-text">Ya llevas ${money(d.montoPagado)} pagados: no puedes bajarlo de ahí.</div>`
          : ""}</div>
      <div class="col-12 col-sm-6"><label class="form-label" for="f-venc">Vencimiento</label>
        <input type="date" class="form-control" id="f-venc" value="${d?.fechaVencimiento ? dateInputValue(d.fechaVencimiento) : ""}"></div>
      <div class="col-12"><label class="form-label" for="f-desc">Nota</label>
        <input type="text" class="form-control" id="f-desc" placeholder="Opcional" value="${esc(d?.descripcion ?? "")}"></div>
      <div class="col-12"><button class="btn btn-primary w-100" id="f-guardar">Guardar</button></div>
    </div>`);

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
      fechaInicio: edicion ? dateInputValue(d.fechaInicio) : dateInputValue(),
      descripcion: body.querySelector("#f-desc").value,
    };
    try {
      if (edicion) await api.put(`/deudas/${d.id}`, payload);
      else await api.post("/deudas", payload);
      closeModal();
      await render(container);
      toast(edicion ? "Deuda actualizada." : "Deuda creada.");
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

// Prestar mas sobre la deuda que ya existe, en vez de abrir otra a la misma persona.
// No mueve el periodo: igual que al crear la deuda, el dinero solo se registra al pagarla.
// Con "a" el mismo formulario corrige una ampliacion ya registrada.
function formAmpliar(container, d, a) {
  const prestamo = d.tipo === "DEBO";
  const edicion = Boolean(a);
  const titulo = edicion
    ? `Editar ampliación — ${d.nombre}`
    : `${prestamo ? "Me prestaron más" : "Presté más"} — ${d.nombre}`;

  const body = openModal(titulo, `
    <div class="row g-3">
      <div class="col-12">
        <div class="text-muted-app" style="font-size:.8125rem">
          Ahora ${prestamo ? "debes" : "te deben"} <span class="numeric">${money(d.saldoPendiente)}</span>
          de <span class="numeric">${money(d.montoOriginal)}</span>.
        </div>
      </div>
      <div class="col-7"><label class="form-label" for="f-monto">${edicion ? "Monto" : "Monto adicional"}</label>
        <input type="number" step="0.01" min="0" class="form-control" id="f-monto" value="${a?.monto ?? ""}" autofocus></div>
      <div class="col-5"><label class="form-label" for="f-fecha">Fecha</label>
        <input type="date" class="form-control" id="f-fecha" value="${dateInputValue(a?.fecha)}"></div>
      <div class="col-12"><label class="form-label" for="f-desc">Nota</label>
        <input type="text" class="form-control" id="f-desc" placeholder="opcional" value="${esc(a?.descripcion ?? "")}">
        <div class="form-text">No registra ningún movimiento en el periodo, solo cambia el total de la deuda.</div></div>
      <div class="col-12"><button class="btn btn-primary w-100" id="f-guardar">${edicion ? "Guardar" : "Ampliar deuda"}</button></div>
    </div>`);

  body.querySelector("#f-guardar").onclick = async () => {
    const payload = {
      monto: Number(body.querySelector("#f-monto").value),
      fecha: body.querySelector("#f-fecha").value,
      descripcion: body.querySelector("#f-desc").value,
    };
    try {
      if (edicion) await api.put(`/deudas/ampliaciones/${a.id}`, payload);
      else await api.post(`/deudas/${d.id}/ampliaciones`, payload);
      closeModal();
      await refrescar(container, edicion ? d : null);
      toast(edicion ? "Ampliación corregida." : "Deuda ampliada.");
    } catch (e) { toast(e.message, "danger"); }
  };
}

// Tras corregir desde el historial conviene volver a el, pero con la deuda recargada:
// el total y el estado acaban de cambiar y la copia de la tarjeta ya esta vieja.
async function refrescar(container, volverAlHistorialDe) {
  await render(container);
  if (!volverAlHistorialDe) return;
  const fresca = deudas.find(x => x.id === volverAlHistorialDe.id);
  if (fresca) await verHistorial(container, fresca);
}

async function verHistorial(container, d) {
  const movimientos = await api.get(`/deudas/${d.id}/movimientos`);
  const body = openModal(`Historial — ${d.nombre}`, `
    <div class="table-responsive">
      <table class="table table-hover">
        <thead><tr><th style="width:5rem">Fecha</th><th>Nota</th><th class="text-end">Monto</th><th style="width:4.5rem"></th></tr></thead>
        <tbody>
          ${movimientos.map(m => `<tr ${m.esAmpliacion ? `data-aid="${m.id}"` : ""}>
              <td class="text-muted-app numeric" style="font-size:.8125rem">${fechaCorta(m.fecha)}</td>
              <td>
                ${esc(m.descripcion) || "<span class='text-muted-app'>—</span>"}
                ${m.esAmpliacion ? `<span class="chip chip-neutral ms-1">Ampliación</span>` : ""}
              </td>
              <td class="text-end numeric fw-medium ${m.esAmpliacion ? "text-muted-app" : DIR[d.tipo].clase}">
                ${m.esAmpliacion ? "+" : ""}${money(m.monto)}
              </td>
              <td class="text-end">${m.esAmpliacion ? `<span class="row-actions">
                <button class="btn btn-icon btn-editar-amp" title="Editar"><i class="bi bi-pencil"></i></button>
                <button class="btn btn-icon danger btn-borrar-amp" title="Eliminar"><i class="bi bi-trash"></i></button>
              </span>` : ""}</td>
            </tr>`).join("") || `<tr><td colspan="4" class="empty-state">Sin movimientos todavía</td></tr>`}
        </tbody>
      </table>
      <div class="form-text mt-2">Los pagos se corrigen desde Movimientos, en el periodo donde se registraron.</div>
    </div>`);

  for (const tr of body.querySelectorAll("tr[data-aid]")) {
    const a = movimientos.find(m => m.esAmpliacion && m.id === Number(tr.dataset.aid));
    tr.querySelector(".btn-editar-amp").onclick = () => formAmpliar(container, d, a);
    tr.querySelector(".btn-borrar-amp").onclick = async () => {
      if (!confirmar(`¿Eliminar esta ampliación de ${money(a.monto)}? La deuda bajará en ese monto.`)) return;
      try {
        await api.del(`/deudas/ampliaciones/${a.id}`);
        closeModal();
        await refrescar(container, d);
        toast("Ampliación eliminada.");
      } catch (e) { toast(e.message, "danger"); }
    };
  }
}

async function eliminar(container, d) {
  if (!confirmar(`¿Eliminar "${d.nombre}"? Los movimientos ya registrados en los periodos se conservan.`)) return;
  try {
    await api.del(`/deudas/${d.id}`);
    await render(container);
    toast("Deuda eliminada.");
  } catch (e) { toast(e.message, "danger"); }
}
