import { api } from "./api.js";
import { money, dateInputValue, toast, confirmar } from "./ui.js";
import { openModal, closeModal } from "./modal.js";

let cursor = new Date(); // mes que se esta mostrando
let periodo = null;
let ingresos = [];
let gastos = [];
let categorias = [];

export async function render(container) {
  categorias = categorias.length ? categorias : await api.get("/categorias");
  await cargarPeriodo(container);
}

async function cargarPeriodo(container) {
  const anio = cursor.getFullYear();
  const mes = cursor.getMonth() + 1;

  try {
    periodo = await api.get(`/periodos/actual?anio=${anio}&mes=${mes}`);
  } catch {
    periodo = null;
  }

  if (!periodo) {
    container.innerHTML = vistaSinPeriodo();
    document.getElementById("btn-prev-mes").onclick = () => cambiarMes(container, -1);
    document.getElementById("btn-next-mes").onclick = () => cambiarMes(container, 1);
    return;
  }

  [ingresos, gastos] = await Promise.all([
    api.get(`/periodos/${periodo.id}/ingresos`),
    api.get(`/periodos/${periodo.id}/gastos`),
  ]);

  container.innerHTML = vista();
  bind(container);
}

function cambiarMes(container, delta) {
  cursor = new Date(cursor.getFullYear(), cursor.getMonth() + delta, 1);
  cargarPeriodo(container);
}

function totales() {
  const totalIngresos = ingresos.reduce((s, i) => s + i.monto, 0);
  const totalGastos = gastos.reduce((s, g) => s + g.monto, 0);
  const saldo = (periodo.saldoInicial ?? 0) + (periodo.sueldoBase ?? 0) + totalIngresos - totalGastos;
  return { totalIngresos, totalGastos, saldo };
}

function vistaSinPeriodo() {
  return `
    <div class="d-flex align-items-center gap-2 mb-3">
      <button class="btn btn-outline-secondary btn-sm" id="btn-prev-mes">←</button>
      <h1 class="h4 mb-0">${nombreMes()}</h1>
      <button class="btn btn-outline-secondary btn-sm" id="btn-next-mes">→</button>
    </div>
    <div class="alert alert-secondary">Este mes todavía no tiene movimientos registrados.</div>`;
}

function nombreMes() {
  return cursor.toLocaleDateString("es-ES", { month: "long", year: "numeric" });
}

function vista() {
  const { totalIngresos, totalGastos, saldo } = totales();
  const filas = [...ingresos.map(i => ({ ...i, __tipo: "ingreso" })), ...gastos.map(g => ({ ...g, __tipo: "gasto" }))]
    .sort((a, b) => new Date(b.fecha) - new Date(a.fecha));

  return `
    <div class="d-flex align-items-center gap-2 mb-3 flex-wrap">
      <button class="btn btn-outline-secondary btn-sm" id="btn-prev-mes">←</button>
      <h1 class="h4 mb-0 text-capitalize">${periodo.nombre}</h1>
      <button class="btn btn-outline-secondary btn-sm" id="btn-next-mes">→</button>
      <button class="btn btn-link btn-sm" id="btn-editar-sueldo">Editar sueldo base</button>
    </div>

    <div class="row g-3 mb-4">
      <div class="col-md-4"><div class="card tile bg-primary-subtle"><div class="card-body">
        <div class="text-muted small">Saldo</div><div class="h4 mb-0">${money(saldo)}</div></div></div></div>
      <div class="col-md-4"><div class="card tile bg-success-subtle"><div class="card-body">
        <div class="text-muted small">Ingresos</div><div class="h4 mb-0">${money(totalIngresos)}</div></div></div></div>
      <div class="col-md-4"><div class="card tile bg-danger-subtle"><div class="card-body">
        <div class="text-muted small">Gastos</div><div class="h4 mb-0">${money(totalGastos)}</div></div></div></div>
    </div>

    <div class="d-flex justify-content-between align-items-center mb-2 flex-wrap gap-2">
      <input type="search" class="form-control" style="max-width: 300px" id="buscar" placeholder="Buscar...">
      <div class="btn-group">
        <button class="btn btn-success btn-sm" id="btn-nuevo-ingreso">+ Ingreso</button>
        <button class="btn btn-danger btn-sm" id="btn-nuevo-gasto">+ Egreso</button>
      </div>
    </div>

    <div class="table-responsive">
      <table class="table table-sm align-middle bg-white">
        <thead><tr><th>Fecha</th><th>Tipo</th><th>Descripción</th><th class="text-end">Monto</th><th></th></tr></thead>
        <tbody id="tabla-movimientos">
          ${filas.map(filaHtml).join("") || `<tr><td colspan="5" class="text-muted text-center py-3">Sin movimientos</td></tr>`}
        </tbody>
      </table>
    </div>`;
}

function filaHtml(m) {
  const esIngreso = m.__tipo === "ingreso";
  return `
    <tr data-id="${m.id}" data-tipo="${m.__tipo}" data-desc="${(m.descripcion || "").toLowerCase()}">
      <td>${m.fecha.slice(0, 10)}</td>
      <td><span class="badge ${esIngreso ? "text-bg-success" : "text-bg-danger"}">${esIngreso ? m.tipo : (m.esSobre ? "Sobre" : "Gasto")}</span></td>
      <td>${m.descripcion}</td>
      <td class="text-end">${esIngreso ? "+" : "-"} ${money(m.monto)}</td>
      <td class="text-end">
        <button class="btn btn-sm btn-outline-secondary btn-editar">✏</button>
        <button class="btn btn-sm btn-outline-danger btn-eliminar">🗑</button>
      </td>
    </tr>`;
}

function bind(container) {
  document.getElementById("btn-prev-mes").onclick = () => cambiarMes(container, -1);
  document.getElementById("btn-next-mes").onclick = () => cambiarMes(container, 1);
  document.getElementById("btn-editar-sueldo").onclick = editarSueldo;
  document.getElementById("btn-nuevo-ingreso").onclick = () => formIngreso(container);
  document.getElementById("btn-nuevo-gasto").onclick = () => formGasto(container);

  document.getElementById("buscar").oninput = (e) => {
    const q = e.target.value.toLowerCase();
    for (const tr of document.querySelectorAll("#tabla-movimientos tr[data-id]"))
      tr.style.display = tr.dataset.desc.includes(q) ? "" : "none";
  };

  for (const tr of document.querySelectorAll("#tabla-movimientos tr[data-id]")) {
    const id = Number(tr.dataset.id);
    const tipo = tr.dataset.tipo;
    tr.querySelector(".btn-editar").onclick = () =>
      tipo === "ingreso" ? formIngreso(container, ingresos.find(i => i.id === id)) : formGasto(container, gastos.find(g => g.id === id));
    tr.querySelector(".btn-eliminar").onclick = () => eliminar(container, tipo, id);
  }
}

function editarSueldo() {
  const body = openModal("Editar sueldo base", `
    <div class="mb-3">
      <label class="form-label">Sueldo base</label>
      <input type="number" step="0.01" min="0" class="form-control" id="f-sueldo" value="${periodo.sueldoBase}">
    </div>
    <button class="btn btn-primary w-100" id="f-guardar">Guardar</button>`);
  body.querySelector("#f-guardar").onclick = async () => {
    try {
      await api.put(`/periodos/${periodo.id}/sueldo`, { sueldoBase: Number(body.querySelector("#f-sueldo").value) });
      closeModal();
      await cargarPeriodo(document.getElementById("view-periodo"));
      toast("Sueldo base actualizado.", "success");
    } catch (e) { toast(e.message, "danger"); }
  };
}

function formIngreso(container, ing) {
  const body = openModal(ing ? "Editar ingreso" : "Nuevo ingreso", `
    <div class="mb-3"><label class="form-label">Monto</label>
      <input type="number" step="0.01" min="0" class="form-control" id="f-monto" value="${ing?.monto ?? ""}"></div>
    <div class="mb-3"><label class="form-label">Fecha</label>
      <input type="date" class="form-control" id="f-fecha" value="${dateInputValue(ing?.fecha)}"></div>
    <div class="mb-3"><label class="form-label">Tipo</label>
      <select class="form-select" id="f-tipo">
        ${["SUELDO", "EXTRA", "OTRO"].map(t => `<option value="${t}" ${ing?.tipo === t ? "selected" : ""}>${t}</option>`).join("")}
      </select></div>
    <div class="mb-3"><label class="form-label">Descripción</label>
      <input type="text" class="form-control" id="f-desc" value="${ing?.descripcion ?? ""}"></div>
    <button class="btn btn-primary w-100" id="f-guardar">Guardar</button>`);

  body.querySelector("#f-guardar").onclick = async () => {
    const payload = {
      monto: Number(body.querySelector("#f-monto").value),
      fecha: body.querySelector("#f-fecha").value,
      tipo: body.querySelector("#f-tipo").value,
      descripcion: body.querySelector("#f-desc").value,
    };
    try {
      if (ing) await api.put(`/ingresos/${ing.id}`, payload);
      else await api.post(`/periodos/${periodo.id}/ingresos`, payload);
      closeModal();
      await cargarPeriodo(container);
      toast("Ingreso guardado.", "success");
    } catch (e) { toast(e.message, "danger"); }
  };
}

function formGasto(container, g) {
  const body = openModal(g ? "Editar egreso" : "Nuevo egreso", `
    <div class="mb-3"><label class="form-label">Monto</label>
      <input type="number" step="0.01" min="0" class="form-control" id="f-monto" value="${g?.monto ?? ""}"></div>
    <div class="mb-3"><label class="form-label">Fecha</label>
      <input type="date" class="form-control" id="f-fecha" value="${dateInputValue(g?.fecha)}"></div>
    <div class="mb-3"><label class="form-label">Categoría</label>
      <select class="form-select" id="f-cat">
        <option value="">(sin categoría)</option>
        ${categorias.map(c => `<option value="${c.id}" ${g?.categoriaId === c.id ? "selected" : ""}>${c.nombre}</option>`).join("")}
      </select></div>
    <div class="mb-3"><label class="form-label">Descripción</label>
      <input type="text" class="form-control" id="f-desc" value="${g?.descripcion ?? ""}"></div>
    <div class="form-check mb-3">
      <input class="form-check-input" type="checkbox" id="f-sobre" ${g?.esSobre ? "checked" : ""}>
      <label class="form-check-label" for="f-sobre">Es un sobre (presupuesto consumible)</label>
    </div>
    <button class="btn btn-primary w-100" id="f-guardar">Guardar</button>`);

  body.querySelector("#f-guardar").onclick = async () => {
    const catVal = body.querySelector("#f-cat").value;
    const payload = {
      monto: Number(body.querySelector("#f-monto").value),
      fecha: body.querySelector("#f-fecha").value,
      categoriaId: catVal ? Number(catVal) : null,
      descripcion: body.querySelector("#f-desc").value,
      esSobre: body.querySelector("#f-sobre").checked,
    };
    try {
      if (g) await api.put(`/gastos/${g.id}`, payload);
      else await api.post(`/periodos/${periodo.id}/gastos`, payload);
      closeModal();
      await cargarPeriodo(container);
      toast("Egreso guardado.", "success");
    } catch (e) { toast(e.message, "danger"); }
  };
}

async function eliminar(container, tipo, id) {
  if (!confirmar("¿Eliminar este movimiento?")) return;
  try {
    await api.del(tipo === "ingreso" ? `/ingresos/${id}` : `/gastos/${id}`);
    await cargarPeriodo(container);
    toast("Eliminado.", "success");
  } catch (e) { toast(e.message, "danger"); }
}
