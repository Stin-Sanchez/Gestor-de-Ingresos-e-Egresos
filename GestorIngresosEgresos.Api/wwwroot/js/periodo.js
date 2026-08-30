import { api } from "./api.js";
import { money, dateInputValue, fechaCorta, esc, toast, confirmar } from "./ui.js";
import { openModal, closeModal } from "./modal.js";

let cursor = new Date(); // mes que se esta mostrando
let periodo = null;
let ingresos = [];
let gastos = [];
let categorias = [];
let chart = null;

export async function render(container) {
  if (!categorias.length) categorias = await api.get("/categorias");
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
    container.innerHTML = `${cabecera(nombreMes(), false)}
      <div class="surface empty-state mt-3">
        <i class="bi bi-calendar-x d-block fs-4 mb-2"></i>
        Este mes no tiene movimientos registrados.
      </div>`;
    bindNav(container);
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

function nombreMes() {
  const s = cursor.toLocaleDateString("es-ES", { month: "long", year: "numeric" });
  return s.charAt(0).toUpperCase() + s.slice(1);
}

function totales() {
  const totalIngresos = ingresos.reduce((s, i) => s + i.monto, 0);
  const totalGastos = gastos.reduce((s, g) => s + g.monto, 0);
  const saldo = (periodo.saldoInicial ?? 0) + (periodo.sueldoBase ?? 0) + totalIngresos - totalGastos;
  return { totalIngresos, totalGastos, saldo };
}

function cabecera(titulo, conSueldo) {
  return `
    <div class="d-flex align-items-center gap-2 flex-wrap">
      <button class="btn btn-icon" id="btn-prev-mes" title="Mes anterior"><i class="bi bi-chevron-left"></i></button>
      <h1 class="h5 mb-0 fw-semibold">${esc(titulo)}</h1>
      <button class="btn btn-icon" id="btn-next-mes" title="Mes siguiente"><i class="bi bi-chevron-right"></i></button>
      ${conSueldo ? `<button class="btn btn-quiet btn-sm ms-auto" id="btn-editar-sueldo">
          <i class="bi bi-pencil me-1"></i> Sueldo base: ${money(periodo.sueldoBase)}
        </button>` : ""}
    </div>`;
}

function vista() {
  const { totalIngresos, totalGastos, saldo } = totales();
  const filas = [...ingresos.map(i => ({ ...i, __tipo: "ingreso" })), ...gastos.map(g => ({ ...g, __tipo: "gasto" }))]
    .sort((a, b) => new Date(b.fecha) - new Date(a.fecha) || b.id - a.id);

  return `
    ${cabecera(periodo.nombre, true)}

    <div class="row g-3 mt-1 mb-3">
      ${tile("Saldo", saldo, saldo < 0 ? "text-neg" : "")}
      ${tile("Ingresos", totalIngresos, "text-pos")}
      ${tile("Gastos", totalGastos, "text-neg")}
    </div>

    <div class="row g-3">
      <div class="col-lg-8">
        <div class="surface">
          <div class="d-flex justify-content-between align-items-center gap-2 p-3 flex-wrap" style="border-bottom:1px solid var(--app-border)">
            <div class="input-group input-group-sm" style="max-width:260px">
              <span class="input-group-text bg-transparent" style="border-color:var(--app-border)"><i class="bi bi-search text-muted-app"></i></span>
              <input type="search" class="form-control" id="buscar" placeholder="Buscar movimiento…">
            </div>
            <div class="d-flex gap-2">
              <button class="btn btn-quiet btn-sm" id="btn-nuevo-ingreso"><i class="bi bi-plus-lg me-1 text-pos"></i>Ingreso</button>
              <button class="btn btn-quiet btn-sm" id="btn-nuevo-gasto"><i class="bi bi-plus-lg me-1 text-neg"></i>Egreso</button>
            </div>
          </div>
          <div class="table-responsive">
            <table class="table table-hover">
              <thead><tr><th style="width:5rem">Fecha</th><th>Descripción</th><th class="text-end">Monto</th><th style="width:4.5rem"></th></tr></thead>
              <tbody id="tabla-movimientos">
                ${filas.map(filaHtml).join("") || `<tr><td colspan="4" class="empty-state">Sin movimientos este mes</td></tr>`}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <div class="col-lg-4">
        <div class="surface p-3">
          <div class="label mb-3">Gastos por categoría</div>
          ${gastos.length ? `<canvas id="chart-categorias" height="220"></canvas>` : `<div class="empty-state py-4">Sin gastos</div>`}
        </div>
      </div>
    </div>`;
}

function tile(label, valor, clase) {
  return `<div class="col-sm-4">
      <div class="surface tile">
        <div class="label">${label}</div>
        <div class="tile-value numeric ${clase}">${money(valor)}</div>
      </div>
    </div>`;
}

function filaHtml(m) {
  const esIngreso = m.__tipo === "ingreso";
  const etiqueta = esIngreso
    ? `<span class="chip chip-neutral">${m.tipo}</span>`
    : m.esSobre ? `<span class="chip chip-neutral">Sobre</span>`
    : m.esAbono ? `<span class="chip chip-neutral">Abono</span>`
    : m.categoriaNombre ? `<span class="text-muted-app" style="font-size:.75rem">${esc(m.categoriaNombre)}</span>` : "";

  return `
    <tr data-id="${m.id}" data-tipo="${m.__tipo}" data-desc="${esc((m.descripcion || "").toLowerCase())}">
      <td class="text-muted-app numeric" style="font-size:.8125rem">${fechaCorta(m.fecha)}</td>
      <td>
        <div class="d-flex align-items-center gap-2">
          <i class="bi ${esIngreso ? "bi-arrow-down-left text-pos" : "bi-arrow-up-right text-neg"}"></i>
          <span>${esc(m.descripcion)}</span>
          ${etiqueta}
        </div>
      </td>
      <td class="text-end numeric fw-medium ${esIngreso ? "text-pos" : ""}">${esIngreso ? "+" : "−"}${money(m.monto)}</td>
      <td class="text-end">
        <span class="row-actions">
          <button class="btn btn-icon btn-editar" title="Editar"><i class="bi bi-pencil"></i></button>
          <button class="btn btn-icon danger btn-eliminar" title="Eliminar"><i class="bi bi-trash"></i></button>
        </span>
      </td>
    </tr>`;
}

function dibujarChart() {
  const ctx = document.getElementById("chart-categorias");
  if (!ctx) return;

  const porCategoria = new Map();
  for (const g of gastos) {
    const key = g.categoriaNombre || "Sin categoría";
    porCategoria.set(key, (porCategoria.get(key) ?? 0) + g.monto);
  }
  const orden = [...porCategoria.entries()].sort((a, b) => b[1] - a[1]);
  const css = getComputedStyle(document.body);

  chart?.destroy();
  chart = new Chart(ctx, {
    type: "doughnut",
    data: {
      labels: orden.map(([k]) => k),
      datasets: [{
        data: orden.map(([, v]) => v),
        backgroundColor: ["#2f6df6", "#0d9c6e", "#e07b26", "#9b5de5", "#d9a406", "#dc3a4b", "#4cc9f0", "#8b93a1"],
        borderWidth: 0,
      }],
    },
    options: {
      cutout: "62%",
      plugins: {
        legend: {
          position: "bottom",
          labels: { color: css.getPropertyValue("--app-muted").trim(), boxWidth: 8, boxHeight: 8, usePointStyle: true, font: { size: 11 } },
        },
        tooltip: { callbacks: { label: (c) => ` ${c.label}: ${money(c.parsed)}` } },
      },
    },
  });
}

function bindNav(container) {
  document.getElementById("btn-prev-mes").onclick = () => cambiarMes(container, -1);
  document.getElementById("btn-next-mes").onclick = () => cambiarMes(container, 1);
}

function bind(container) {
  bindNav(container);
  document.getElementById("btn-editar-sueldo").onclick = () => editarSueldo(container);
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

  dibujarChart();
}

function editarSueldo(container) {
  const body = openModal("Sueldo base del mes", `
    <div class="mb-3">
      <label class="form-label" for="f-sueldo">Monto</label>
      <input type="number" step="0.01" min="0" class="form-control" id="f-sueldo" value="${periodo.sueldoBase}">
    </div>
    <button class="btn btn-primary w-100" id="f-guardar">Guardar</button>`);

  body.querySelector("#f-guardar").onclick = async () => {
    try {
      await api.put(`/periodos/${periodo.id}/sueldo`, { sueldoBase: Number(body.querySelector("#f-sueldo").value) });
      closeModal();
      await cargarPeriodo(container);
      toast("Sueldo base actualizado.");
    } catch (e) { toast(e.message, "danger"); }
  };
}

function formIngreso(container, ing) {
  const body = openModal(ing ? "Editar ingreso" : "Nuevo ingreso", `
    <div class="row g-3">
      <div class="col-7"><label class="form-label" for="f-monto">Monto</label>
        <input type="number" step="0.01" min="0" class="form-control" id="f-monto" value="${ing?.monto ?? ""}" autofocus></div>
      <div class="col-5"><label class="form-label" for="f-fecha">Fecha</label>
        <input type="date" class="form-control" id="f-fecha" value="${dateInputValue(ing?.fecha)}"></div>
      <div class="col-12"><label class="form-label" for="f-tipo">Tipo</label>
        <select class="form-select" id="f-tipo">
          ${["SUELDO", "EXTRA", "OTRO"].map(t => `<option value="${t}" ${ing?.tipo === t ? "selected" : ""}>${t}</option>`).join("")}
        </select></div>
      <div class="col-12"><label class="form-label" for="f-desc">Descripción</label>
        <input type="text" class="form-control" id="f-desc" value="${esc(ing?.descripcion ?? "")}"></div>
      <div class="col-12"><button class="btn btn-primary w-100" id="f-guardar">Guardar</button></div>
    </div>`);

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
      toast("Ingreso guardado.");
    } catch (e) { toast(e.message, "danger"); }
  };
}

function formGasto(container, g) {
  const body = openModal(g ? "Editar egreso" : "Nuevo egreso", `
    <div class="row g-3">
      <div class="col-7"><label class="form-label" for="f-monto">Monto</label>
        <input type="number" step="0.01" min="0" class="form-control" id="f-monto" value="${g?.monto ?? ""}" autofocus></div>
      <div class="col-5"><label class="form-label" for="f-fecha">Fecha</label>
        <input type="date" class="form-control" id="f-fecha" value="${dateInputValue(g?.fecha)}"></div>
      <div class="col-12"><label class="form-label" for="f-cat">Categoría</label>
        <select class="form-select" id="f-cat">
          <option value="">Sin categoría</option>
          ${categorias.map(c => `<option value="${c.id}" ${g?.categoriaId === c.id ? "selected" : ""}>${esc(c.nombre)}</option>`).join("")}
        </select></div>
      <div class="col-12"><label class="form-label" for="f-desc">Descripción</label>
        <input type="text" class="form-control" id="f-desc" value="${esc(g?.descripcion ?? "")}"></div>
      <div class="col-12">
        <div class="form-check">
          <input class="form-check-input" type="checkbox" id="f-sobre" ${g?.esSobre ? "checked" : ""}>
          <label class="form-check-label" for="f-sobre" style="font-size:.875rem">
            Es un sobre <span class="text-muted-app">— presupuesto que se consume durante el mes</span>
          </label>
        </div>
      </div>
      <div class="col-12"><button class="btn btn-primary w-100" id="f-guardar">Guardar</button></div>
    </div>`);

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
      toast("Egreso guardado.");
    } catch (e) { toast(e.message, "danger"); }
  };
}

async function eliminar(container, tipo, id) {
  if (!confirmar("¿Eliminar este movimiento?")) return;
  try {
    await api.del(tipo === "ingreso" ? `/ingresos/${id}` : `/gastos/${id}`);
    await cargarPeriodo(container);
    toast("Movimiento eliminado.");
  } catch (e) { toast(e.message, "danger"); }
}
