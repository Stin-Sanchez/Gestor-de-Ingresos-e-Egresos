import { api } from "./api.js";
import { money, dateInputValue, fechaCorta, esc, toast, confirmar } from "./ui.js";
import { openModal, closeModal } from "./modal.js";

const POR_PAGINA = 12;

let cursor = new Date(); // mes que se esta mostrando
let periodo = null;
let ingresos = [];
let gastos = [];
let categorias = [];
let chart = null;
let busqueda = "";
let pagina = 1;

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
    container.innerHTML = `${cabecera(nombreMes())}
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

  busqueda = "";
  pagina = 1;
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
  const saldo = (periodo.saldoInicial ?? 0) + totalIngresos - totalGastos;
  return { totalIngresos, totalGastos, saldo };
}

// Ingresos y gastos se muestran juntos, ordenados por fecha, como una sola cuenta.
function movimientos() {
  const todos = [
    ...ingresos.map(i => ({ ...i, __tipo: "ingreso" })),
    ...gastos.map(g => ({ ...g, __tipo: "gasto" })),
  ].sort((a, b) => new Date(b.fecha) - new Date(a.fecha) || b.id - a.id);

  if (!busqueda) return todos;
  const q = busqueda.toLowerCase();
  return todos.filter(m =>
    (m.descripcion || "").toLowerCase().includes(q) ||
    (m.categoriaNombre || "").toLowerCase().includes(q));
}

function cabecera(titulo) {
  return `
    <div class="d-flex align-items-center gap-2 flex-wrap">
      <button class="btn btn-icon" id="btn-prev-mes" title="Mes anterior"><i class="bi bi-chevron-left"></i></button>
      <h1 class="h5 mb-0 fw-semibold">${esc(titulo)}</h1>
      <button class="btn btn-icon" id="btn-next-mes" title="Mes siguiente"><i class="bi bi-chevron-right"></i></button>
    </div>`;
}

function vista() {
  const { totalIngresos, totalGastos, saldo } = totales();

  return `
    ${cabecera(periodo.nombre)}

    <div class="row g-3 mt-1 mb-3">
      ${tile("Saldo", saldo, saldo < 0 ? "text-neg" : "")}
      ${tile("Ingresos", totalIngresos, "text-pos")}
      ${tile("Gastos", totalGastos, "text-neg")}
    </div>

    <div class="row g-3">
      <div class="col-12 col-xl-8">
        <div class="surface">
          <div class="d-flex justify-content-between align-items-center gap-2 p-3 flex-wrap" style="border-bottom:1px solid var(--app-border)">
            <div class="input-group input-group-sm" style="max-width:260px">
              <span class="input-group-text bg-transparent" style="border-color:var(--app-border)"><i class="bi bi-search text-muted-app"></i></span>
              <input type="search" class="form-control" id="buscar" placeholder="Buscar…">
            </div>
            <div class="d-flex gap-2">
              <button class="btn btn-quiet btn-sm" id="btn-nuevo-ingreso"><i class="bi bi-plus-lg me-1 text-pos"></i>Ingreso</button>
              <button class="btn btn-quiet btn-sm" id="btn-nuevo-gasto"><i class="bi bi-plus-lg me-1 text-neg"></i>Egreso</button>
            </div>
          </div>
          <div id="zona-tabla">${tablaHtml()}</div>
        </div>
      </div>

      <div class="col-12 col-xl-4">
        <div class="surface p-3">
          <div class="label mb-3">Gastos por categoría</div>
          ${gastos.length ? `<canvas id="chart-categorias" height="220"></canvas>` : `<div class="empty-state py-4">Sin gastos</div>`}
        </div>
      </div>
    </div>`;
}

function tile(label, valor, clase) {
  return `<div class="col-12 col-sm-4">
      <div class="surface tile">
        <div class="label">${label}</div>
        <div class="tile-value numeric ${clase}">${money(valor)}</div>
      </div>
    </div>`;
}

function tablaHtml() {
  const filas = movimientos();
  const paginas = Math.max(1, Math.ceil(filas.length / POR_PAGINA));
  if (pagina > paginas) pagina = paginas;

  const desde = (pagina - 1) * POR_PAGINA;
  const pagina_actual = filas.slice(desde, desde + POR_PAGINA);

  return `
    <div class="table-responsive">
      <table class="table table-hover tabla-compacta">
        <thead><tr><th style="width:5rem">Fecha</th><th>Descripción</th><th class="text-end">Monto</th><th style="width:4.5rem"></th></tr></thead>
        <tbody id="tabla-movimientos">
          ${pagina_actual.map(filaHtml).join("")
            || `<tr><td colspan="4" class="empty-state">${busqueda ? "Ningún movimiento coincide" : "Sin movimientos este mes"}</td></tr>`}
        </tbody>
      </table>
    </div>
    ${filas.length > POR_PAGINA ? paginacionHtml(filas.length, paginas, desde, pagina_actual.length) : ""}`;
}

function paginacionHtml(total, paginas, desde, enPagina) {
  return `
    <div class="d-flex justify-content-between align-items-center gap-2 p-3 flex-wrap" style="border-top:1px solid var(--app-border)">
      <span class="text-muted-app numeric" style="font-size:.8125rem">${desde + 1}–${desde + enPagina} de ${total}</span>
      <div class="d-flex align-items-center gap-1">
        <button class="btn btn-icon" id="btn-pag-prev" ${pagina === 1 ? "disabled" : ""} title="Anterior"><i class="bi bi-chevron-left"></i></button>
        <span class="numeric px-2" style="font-size:.8125rem">${pagina} / ${paginas}</span>
        <button class="btn btn-icon" id="btn-pag-next" ${pagina === paginas ? "disabled" : ""} title="Siguiente"><i class="bi bi-chevron-right"></i></button>
      </div>
    </div>`;
}

function filaHtml(m) {
  const esIngreso = m.__tipo === "ingreso";
  const ligadoADeuda = esIngreso ? m.esCobro : m.esAbono;

  const etiqueta = ligadoADeuda ? `<span class="chip chip-neutral">${esIngreso ? "Cobro" : "Abono"}</span>`
    : esIngreso ? `<span class="chip chip-neutral">${m.tipo}</span>`
    : m.esSobre ? `<span class="chip chip-neutral">Sobre</span>`
    : m.categoriaNombre ? `<span class="text-muted-app" style="font-size:.75rem">${esc(m.categoriaNombre)}</span>` : "";

  return `
    <tr data-id="${m.id}" data-tipo="${m.__tipo}">
      <td class="text-muted-app numeric" style="font-size:.8125rem">${fechaCorta(m.fecha)}</td>
      <td>
        <div class="d-flex align-items-center gap-2 flex-wrap">
          <i class="bi ${esIngreso ? "bi-arrow-down-left text-pos" : "bi-arrow-up-right text-neg"}"></i>
          <span class="text-break">${esc(m.descripcion)}</span>
          ${etiqueta}
        </div>
      </td>
      <td class="text-end numeric fw-medium ${esIngreso ? "text-pos" : ""}">${esIngreso ? "+" : "−"}${money(m.monto)}</td>
      <td class="text-end">
        <span class="row-actions">
          ${ligadoADeuda ? "" : `<button class="btn btn-icon btn-editar" title="Editar"><i class="bi bi-pencil"></i></button>`}
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

// Solo repinta la tabla: el grafico y la busqueda no deben perderse al pasar de pagina.
function repintarTabla(container) {
  container.querySelector("#zona-tabla").innerHTML = tablaHtml();
  bindTabla(container);
}

function bindTabla(container) {
  container.querySelector("#btn-pag-prev")?.addEventListener("click", () => {
    if (pagina > 1) { pagina--; repintarTabla(container); }
  });
  container.querySelector("#btn-pag-next")?.addEventListener("click", () => {
    pagina++; repintarTabla(container);
  });

  for (const tr of container.querySelectorAll("#tabla-movimientos tr[data-id]")) {
    const id = Number(tr.dataset.id);
    const tipo = tr.dataset.tipo;
    tr.querySelector(".btn-editar")?.addEventListener("click", () =>
      tipo === "ingreso" ? formIngreso(container, ingresos.find(i => i.id === id)) : formGasto(container, gastos.find(g => g.id === id)));
    tr.querySelector(".btn-eliminar").onclick = () => eliminar(container, tipo, id);
  }
}

function bind(container) {
  bindNav(container);
  document.getElementById("btn-nuevo-ingreso").onclick = () => formIngreso(container);
  document.getElementById("btn-nuevo-gasto").onclick = () => formGasto(container);

  document.getElementById("buscar").oninput = (e) => {
    busqueda = e.target.value;
    pagina = 1;
    repintarTabla(container);
  };

  bindTabla(container);
  dibujarChart();
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
        </select>
        <div class="form-text">Si cobras varias veces al mes, registra cada pago por separado (quincena, fin de mes…).</div></div>
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
