import { api } from "./api.js";
import { money, dateInputValue, fechaCorta, esc, toast, confirmar, ESTADO_LABEL } from "./ui.js";
import { openModal, closeModal } from "./modal.js";

let cursor = new Date(); // mes que se esta mostrando
let periodo = null;
let sobres = [];
let seleccionadoId = null;
let consumos = [];

const cerrado = () => periodo?.estado === "CERRADO";

export async function render(container) {
  try {
    periodo = await api.get(`/periodos/actual?anio=${cursor.getFullYear()}&mes=${cursor.getMonth() + 1}`);
  } catch {
    periodo = null;
  }

  if (!periodo) {
    sobres = [];
    consumos = [];
    container.innerHTML = `${cabecera(nombreMes())}
      <div class="surface empty-state mt-3"><i class="bi bi-calendar-x d-block fs-4 mb-2"></i>Este mes no tiene periodo.</div>`;
    bindNav(container);
    return;
  }

  sobres = await api.get(`/periodos/${periodo.id}/sobres`);
  const sel = sobres.find(s => s.gastoId === seleccionadoId) ?? sobres[0] ?? null;
  seleccionadoId = sel?.gastoId ?? null;
  consumos = sel ? await api.get(`/gastos/${sel.gastoId}/consumos`) : [];

  container.innerHTML = vista(sel);
  bind(container);
}

function cambiarMes(container, delta) {
  cursor = new Date(cursor.getFullYear(), cursor.getMonth() + delta, 1);
  seleccionadoId = null; // los sobres son de otro periodo: la seleccion anterior ya no existe
  render(container);
}

function nombreMes() {
  const s = cursor.toLocaleDateString("es-ES", { month: "long", year: "numeric" });
  return s.charAt(0).toUpperCase() + s.slice(1);
}

// Mismo navegador de meses que Movimientos: los sobres de un periodo cerrado se
// consultan igual que los del actual, solo que sin poder tocarlos.
function cabecera(titulo, extra = "") {
  return `
    <div class="d-flex align-items-center justify-content-between gap-2 flex-wrap">
      <div class="d-flex align-items-center gap-2 flex-wrap">
        <button class="btn btn-icon" id="btn-prev-mes" title="Mes anterior"><i class="bi bi-chevron-left"></i></button>
        <h1 class="h5 mb-0 fw-semibold">${esc(titulo)}</h1>
        <button class="btn btn-icon" id="btn-next-mes" title="Mes siguiente"><i class="bi bi-chevron-right"></i></button>
        ${cerrado() ? `<span class="chip chip-neutral"><i class="bi bi-lock me-1"></i>Cerrado</span>` : ""}
      </div>
      ${extra}
    </div>`;
}

function vista(sel) {
  const conteo = `<span class="text-muted-app" style="font-size:.8125rem">${sobres.length} sobre${sobres.length === 1 ? "" : "s"}</span>`;

  if (!sobres.length) {
    return `${cabecera(periodo.nombre, conteo)}
      <div class="surface empty-state mt-3">
        <i class="bi bi-wallet2 d-block fs-4 mb-2"></i>
        Este periodo no tiene sobres.<br>
        <span style="font-size:.8125rem">Marca un egreso como &ldquo;sobre&rdquo; en Movimientos para verlo aquí.</span>
      </div>`;
  }

  return `
    ${cabecera(periodo.nombre, conteo)}
    ${cerrado() ? avisoCerrado() : ""}
    <div class="row g-3 mt-1">
      <div class="col-12 col-lg-5 col-xxl-4">
        <div class="d-flex flex-column gap-2">${sobres.map(sobreCard).join("")}</div>
      </div>
      <div class="col-12 col-lg-7 col-xxl-8">${sel ? detalle(sel) : ""}</div>
    </div>`;
}

function avisoCerrado() {
  return `<div class="surface p-3 mt-3 d-flex align-items-center gap-2" style="font-size:.8125rem">
      <i class="bi bi-lock text-muted-app"></i>
      <span class="text-muted-app">Periodo cerrado: puedes consultarlo, pero no registrar consumos. Reábrelo desde Ajustes para editarlo.</span>
    </div>`;
}

function sobreCard(s) {
  const activo = s.gastoId === seleccionadoId;
  return `
    <div class="surface sobre-card p-3 ${activo ? "selected" : ""}" data-id="${s.gastoId}">
      <div class="d-flex justify-content-between align-items-start gap-2 mb-1">
        <div class="min-w-0">
          <div class="fw-medium text-truncate">${esc(s.titulo)}</div>
          <div class="text-muted-app" style="font-size:.75rem">${esc(s.categoriaNombre || "Sin categoría")}</div>
        </div>
        <span class="chip chip-${s.estado}">${ESTADO_LABEL[s.estado]}</span>
      </div>
      <div class="progress my-2"><div class="progress-bar estado-${s.estado}" style="width:${Math.min(s.porcentajeMostrado, 100)}%"></div></div>
      <div class="d-flex justify-content-between numeric" style="font-size:.8125rem">
        <span class="text-muted-app">${money(s.gastado)} de ${money(s.limite)}</span>
        <span>${s.porcentajeMostrado}%</span>
      </div>
    </div>`;
}

function detalle(s) {
  return `
    <div class="surface">
      <div class="p-3" style="border-bottom:1px solid var(--app-border)">
        <div class="d-flex justify-content-between align-items-start gap-3 flex-wrap">
          <div>
            <div class="label">Disponible en &ldquo;${esc(s.titulo)}&rdquo;</div>
            <div class="tile-value numeric ${s.disponible <= 0 ? "text-neg" : "text-pos"}">${money(s.disponible)}</div>
            <div class="text-muted-app numeric mt-1" style="font-size:.8125rem">
              Consumido ${money(s.gastado)} · Límite ${money(s.limite)}
            </div>
          </div>
          <button class="btn btn-primary btn-sm" id="btn-nuevo-consumo" ${s.disponible <= 0 || cerrado() ? "disabled" : ""}>
            <i class="bi bi-plus-lg me-1"></i>Consumo
          </button>
        </div>
        <div class="progress mt-3"><div class="progress-bar estado-${s.estado}" style="width:${Math.min(s.porcentajeMostrado, 100)}%"></div></div>
      </div>
      <div class="table-responsive">
        <table class="table table-hover tabla-compacta">
          <thead><tr><th style="width:5rem">Fecha</th><th>Descripción</th><th class="text-end">Monto</th><th style="width:4.5rem"></th></tr></thead>
          <tbody>
            ${consumos.map(c => `
              <tr data-cid="${c.id}">
                <td class="text-muted-app numeric" style="font-size:.8125rem">${fechaCorta(c.fecha)}</td>
                <td>${esc(c.descripcion)}</td>
                <td class="text-end numeric fw-medium">${money(c.monto)}</td>
                <td class="text-end">${cerrado() ? "" : `<span class="row-actions">
                  <button class="btn btn-icon btn-editar" title="Editar"><i class="bi bi-pencil"></i></button>
                  <button class="btn btn-icon danger btn-eliminar" title="Eliminar"><i class="bi bi-trash"></i></button>
                </span>`}</td>
              </tr>`).join("") || `<tr><td colspan="4" class="empty-state">Sin consumos registrados</td></tr>`}
          </tbody>
        </table>
      </div>
    </div>`;
}

function bindNav(container) {
  container.querySelector("#btn-prev-mes").onclick = () => cambiarMes(container, -1);
  container.querySelector("#btn-next-mes").onclick = () => cambiarMes(container, 1);
}

function bind(container) {
  bindNav(container);

  for (const card of container.querySelectorAll(".sobre-card")) {
    card.onclick = async () => {
      seleccionadoId = Number(card.dataset.id);
      await render(container);
    };
  }

  container.querySelector("#btn-nuevo-consumo")?.addEventListener("click", () => formConsumo(container));

  for (const tr of container.querySelectorAll("tr[data-cid]")) {
    const id = Number(tr.dataset.cid);
    tr.querySelector(".btn-editar")?.addEventListener("click", () => formConsumo(container, consumos.find(c => c.id === id)));
    tr.querySelector(".btn-eliminar")?.addEventListener("click", () => eliminar(container, id));
  }
}

function formConsumo(container, c) {
  const body = openModal(c ? "Editar consumo" : "Nuevo consumo", `
    <div class="row g-3">
      <div class="col-7"><label class="form-label" for="f-monto">Monto</label>
        <input type="number" step="0.01" min="0" class="form-control" id="f-monto" value="${c?.monto ?? ""}" autofocus></div>
      <div class="col-5"><label class="form-label" for="f-fecha">Fecha</label>
        <input type="date" class="form-control" id="f-fecha" value="${dateInputValue(c?.fecha)}"></div>
      <div class="col-12"><label class="form-label" for="f-desc">Descripción</label>
        <input type="text" class="form-control" id="f-desc" value="${esc(c?.descripcion ?? "")}"></div>
      <div class="col-12"><button class="btn btn-primary w-100" id="f-guardar">Guardar</button></div>
    </div>`);

  body.querySelector("#f-guardar").onclick = async () => {
    const payload = {
      monto: Number(body.querySelector("#f-monto").value),
      fecha: body.querySelector("#f-fecha").value,
      descripcion: body.querySelector("#f-desc").value,
    };
    try {
      const resultado = c
        ? await api.put(`/consumos/${c.id}`, payload)
        : await api.post(`/gastos/${seleccionadoId}/consumos`, payload);
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
    toast("Consumo eliminado.");
  } catch (e) { toast(e.message, "danger"); }
}
