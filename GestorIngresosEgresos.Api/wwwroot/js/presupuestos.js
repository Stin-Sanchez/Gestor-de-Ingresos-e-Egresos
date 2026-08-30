import { api } from "./api.js";
import { money, dateInputValue, toast, confirmar, ESTADO_LABEL } from "./ui.js";
import { openModal, closeModal } from "./modal.js";

let sobres = [];
let seleccionado = null;
let consumos = [];
let chart = null;

export async function render(container) {
  const hoy = new Date();
  let periodo;
  try {
    periodo = await api.get(`/periodos/actual?anio=${hoy.getFullYear()}&mes=${hoy.getMonth() + 1}`);
  } catch {
    periodo = null;
  }

  if (!periodo) {
    container.innerHTML = `<div class="alert alert-secondary">No hay periodo activo este mes.</div>`;
    return;
  }

  sobres = await api.get(`/periodos/${periodo.id}/sobres`);
  seleccionado = sobres.find(s => s.gastoId === seleccionado?.gastoId) ?? sobres[0] ?? null;
  consumos = seleccionado ? await api.get(`/gastos/${seleccionado.gastoId}/consumos`) : [];

  container.innerHTML = vista();
  bind(container);
}

function vista() {
  return `
    <h1 class="h4 mb-3">📦 Presupuestos (sobres)</h1>
    ${sobres.length === 0 ? `<div class="alert alert-secondary">Marca un egreso como "sobre" en la vista de Ingresos y Egresos para verlo aquí.</div>` : ""}
    <div class="row g-3">
      <div class="col-md-5">
        <div class="d-flex flex-column gap-2" id="lista-sobres">
          ${sobres.map(sobreCardHtml).join("")}
        </div>
      </div>
      <div class="col-md-7">
        ${seleccionado ? detalleHtml() : ""}
      </div>
    </div>`;
}

function sobreCardHtml(s) {
  const activo = s.gastoId === seleccionado?.gastoId;
  return `
    <div class="card sobre-card ${activo ? "selected" : ""}" data-id="${s.gastoId}">
      <div class="card-body">
        <div class="d-flex justify-content-between">
          <strong>${s.titulo}</strong>
          <span class="badge badge-estado-${s.estado}">${ESTADO_LABEL[s.estado]}</span>
        </div>
        <div class="text-muted small mb-2">${s.categoriaNombre || "Sin categoría"}</div>
        <div class="progress mb-1"><div class="progress-bar estado-${s.estado}" style="width:${Math.min(s.porcentajeMostrado, 100)}%"></div></div>
        <div class="d-flex justify-content-between small">
          <span>${money(s.gastado)} de ${money(s.limite)}</span>
          <span>${s.porcentajeMostrado}%</span>
        </div>
      </div>
    </div>`;
}

function detalleHtml() {
  return `
    <div class="card">
      <div class="card-body">
        <div class="d-flex justify-content-between align-items-center mb-3">
          <h2 class="h5 mb-0">${seleccionado.titulo} — Disponible: ${money(seleccionado.disponible)}</h2>
          <button class="btn btn-primary btn-sm" id="btn-nuevo-consumo">+ Consumo</button>
        </div>
        <canvas id="chart-sobre" height="120"></canvas>
        <table class="table table-sm mt-3">
          <thead><tr><th>Fecha</th><th>Descripción</th><th class="text-end">Monto</th><th></th></tr></thead>
          <tbody>
            ${consumos.map(c => `
              <tr data-id="${c.id}">
                <td>${c.fecha.slice(0, 10)}</td><td>${c.descripcion}</td>
                <td class="text-end">${money(c.monto)}</td>
                <td class="text-end">
                  <button class="btn btn-sm btn-outline-secondary btn-editar">✏</button>
                  <button class="btn btn-sm btn-outline-danger btn-eliminar">🗑</button>
                </td>
              </tr>`).join("") || `<tr><td colspan="4" class="text-muted text-center py-3">Sin consumos</td></tr>`}
          </tbody>
        </table>
      </div>
    </div>`;
}

function dibujarChart() {
  if (!seleccionado) return;
  const ctx = document.getElementById("chart-sobre");
  if (!ctx) return;
  chart?.destroy();
  chart = new Chart(ctx, {
    type: "doughnut",
    data: {
      labels: ["Consumido", "Disponible"],
      datasets: [{
        data: [seleccionado.gastado, Math.max(seleccionado.disponible, 0)],
        backgroundColor: ["#fd7e14", "#198754"],
      }],
    },
    options: { plugins: { legend: { position: "bottom" } } },
  });
}

function bind(container) {
  for (const card of document.querySelectorAll("#lista-sobres .sobre-card")) {
    card.onclick = async () => {
      seleccionado = sobres.find(s => s.gastoId === Number(card.dataset.id));
      await render(container);
    };
  }

  const btnNuevo = document.getElementById("btn-nuevo-consumo");
  if (btnNuevo) btnNuevo.onclick = () => formConsumo(container);

  for (const tr of document.querySelectorAll("tbody tr[data-id]")) {
    const id = Number(tr.dataset.id);
    tr.querySelector(".btn-editar").onclick = () => formConsumo(container, consumos.find(c => c.id === id));
    tr.querySelector(".btn-eliminar").onclick = () => eliminar(container, id);
  }

  dibujarChart();
}

function formConsumo(container, c) {
  const body = openModal(c ? "Editar consumo" : "Nuevo consumo", `
    <div class="mb-3"><label class="form-label">Monto</label>
      <input type="number" step="0.01" min="0" class="form-control" id="f-monto" value="${c?.monto ?? ""}"></div>
    <div class="mb-3"><label class="form-label">Fecha</label>
      <input type="date" class="form-control" id="f-fecha" value="${dateInputValue(c?.fecha)}"></div>
    <div class="mb-3"><label class="form-label">Descripción</label>
      <input type="text" class="form-control" id="f-desc" value="${c?.descripcion ?? ""}"></div>
    <button class="btn btn-primary w-100" id="f-guardar">Guardar</button>`);

  body.querySelector("#f-guardar").onclick = async () => {
    const payload = {
      monto: Number(body.querySelector("#f-monto").value),
      fecha: body.querySelector("#f-fecha").value,
      descripcion: body.querySelector("#f-desc").value,
    };
    try {
      const resultado = c
        ? await api.put(`/consumos/${c.id}`, payload)
        : await api.post(`/gastos/${seleccionado.gastoId}/consumos`, payload);
      closeModal();
      await render(container);
      toast(resultado.aviso ?? "Consumo guardado.", resultado.aviso ? "warning" : "success");
    } catch (e) { toast(e.message, "danger"); }
  };
}

async function eliminar(container, id) {
  if (!confirmar("¿Eliminar este consumo?")) return;
  try {
    await api.del(`/consumos/${id}`);
    await render(container);
    toast("Eliminado.", "success");
  } catch (e) { toast(e.message, "danger"); }
}
