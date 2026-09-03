import { api } from "./api.js";
import { esc, toast, confirmar } from "./ui.js";
import { openModal, closeModal } from "./modal.js";
import { avatarHtml, refrescarUsuario } from "./sesion.js";

let perfil = null;
let config = null;
let periodos = [];

export async function render(container) {
  [perfil, config, periodos] = await Promise.all([
    api.get("/perfil"),
    api.get("/periodos/config"),
    api.get("/periodos"),
  ]);
  container.innerHTML = vista();
  bind(container);
}

function vista() {
  return `
    <h1 class="h5 fw-semibold mb-3">Ajustes</h1>

    <div class="surface p-3 p-md-4 mb-3">
      <div class="label mb-3">Perfil</div>
      <div class="d-flex align-items-center gap-3 flex-wrap mb-4">
        ${avatarHtml(perfil, "avatar-lg")}
        <div>
          <div class="fw-medium">${esc(perfil.username)}</div>
          <div class="text-muted-app" style="font-size:.8125rem">${esc(perfil.email || "Sin correo")}</div>
          <div class="d-flex gap-2 mt-2">
            <label class="btn btn-quiet btn-sm mb-0">
              <i class="bi bi-upload me-1"></i>Cambiar foto
              <input type="file" id="in-avatar" accept="image/png,image/jpeg,image/gif,image/webp" hidden>
            </label>
            ${perfil.avatar ? `<button class="btn btn-quiet btn-sm" id="btn-quitar-avatar"><i class="bi bi-trash"></i></button>` : ""}
          </div>
          <div class="form-text">PNG, JPG, GIF o WEBP. Máximo 2 MB.</div>
        </div>
      </div>

      <form id="form-perfil" class="row g-3" style="max-width:26rem">
        <div class="col-12">
          <label class="form-label" for="in-email">Correo</label>
          <input type="email" class="form-control" id="in-email" value="${esc(perfil.email ?? "")}" placeholder="opcional">
        </div>
        <div class="col-12"><button class="btn btn-primary btn-sm" type="submit">Guardar cambios</button></div>
      </form>
    </div>

    <div class="surface p-3 p-md-4 mb-3">
      <div class="label mb-3">Contraseña</div>
      <form id="form-password" class="row g-3" style="max-width:26rem">
        <div class="col-12">
          <label class="form-label" for="in-actual">Contraseña actual</label>
          <input type="password" class="form-control" id="in-actual" autocomplete="current-password" required>
        </div>
        <div class="col-12">
          <label class="form-label" for="in-nueva">Nueva contraseña</label>
          <input type="password" class="form-control" id="in-nueva" autocomplete="new-password" required>
          <div class="form-text">Mínimo 8 caracteres.</div>
        </div>
        <div class="col-12"><button class="btn btn-primary btn-sm" type="submit">Cambiar contraseña</button></div>
      </form>
    </div>

    <div class="surface p-3 p-md-4 mb-3">
      <div class="label mb-3">Periodos</div>

      <form id="form-periodos" class="row g-3 mb-4" style="max-width:34rem">
        <div class="col-12 col-sm-6">
          <label class="form-label" for="in-corte">Día de corte</label>
          <input type="number" min="1" max="31" class="form-control" id="in-corte" value="${config.diaCorte}">
          <div class="form-text">El periodo arranca ese día del mes. 1 = mes calendario.</div>
        </div>
        <div class="col-12 col-sm-6">
          <label class="form-label" for="in-gracia">Días de gracia</label>
          <input type="number" min="0" max="28" class="form-control" id="in-gracia" value="${config.diasGracia}">
          <div class="form-text">Margen tras el fin antes de cerrarse solo.</div>
        </div>
        <div class="col-12">
          <button class="btn btn-primary btn-sm" type="submit">Guardar configuración</button>
          <div class="form-text">Solo afecta a los periodos que se creen después; los existentes conservan sus fechas.</div>
        </div>
      </form>

      <div class="label mb-2">Historial</div>
      <div class="table-responsive">
        <table class="table table-hover tabla-compacta">
          <thead><tr><th>Periodo</th><th>Rango</th><th>Estado</th><th style="width:6rem"></th></tr></thead>
          <tbody>
            ${periodos.map(filaPeriodo).join("") || `<tr><td colspan="4" class="empty-state">Sin periodos todavía</td></tr>`}
          </tbody>
        </table>
      </div>
    </div>

    <div class="surface p-3 p-md-4">
      <div class="d-flex justify-content-between align-items-start gap-3 flex-wrap">
        <div>
          <div class="label mb-1">Verificación en dos pasos</div>
          <div style="font-size:.875rem">
            ${perfil.dobleFactor
              ? `<span class="chip chip-OK"><i class="bi bi-shield-check me-1"></i>Activa</span>`
              : `<span class="chip chip-neutral">Desactivada</span>`}
          </div>
          <div class="text-muted-app mt-2" style="font-size:.8125rem;max-width:34rem">
            Pide un código de tu app de autenticación (Google Authenticator, Authy, 1Password…)
            además de la contraseña cada vez que inicias sesión.
          </div>
        </div>
        ${perfil.dobleFactor
          ? `<button class="btn btn-quiet btn-sm" id="btn-2fa-off">Desactivar</button>`
          : `<button class="btn btn-primary btn-sm" id="btn-2fa-on"><i class="bi bi-shield-lock me-1"></i>Activar</button>`}
      </div>
    </div>`;
}

function rango(p) {
  const f = (iso) => new Date(iso).toLocaleDateString("es-ES", { day: "2-digit", month: "short" });
  return `${f(p.fechaInicio)} – ${f(p.fechaFin)}`;
}

function filaPeriodo(p) {
  return `
    <tr data-pid="${p.id}">
      <td>${esc(p.nombre)}${p.esActual ? ` <span class="chip chip-neutral">Actual</span>` : ""}</td>
      <td class="text-muted-app numeric" style="font-size:.8125rem">${rango(p)}</td>
      <td>${p.estado === "CERRADO"
        ? `<span class="chip chip-neutral"><i class="bi bi-lock me-1"></i>Cerrado</span>`
        : `<span class="chip chip-OK">Abierto</span>`}</td>
      <td class="text-end">
        <button class="btn btn-quiet btn-sm ${p.estado === "CERRADO" ? "btn-reabrir" : "btn-cerrar"}">
          ${p.estado === "CERRADO" ? "Reabrir" : "Cerrar"}
        </button>
      </td>
    </tr>`;
}

function bind(container) {
  container.querySelector("#form-periodos").onsubmit = async (e) => {
    e.preventDefault();
    try {
      await api.put("/periodos/config", {
        diaCorte: Number(container.querySelector("#in-corte").value),
        diasGracia: Number(container.querySelector("#in-gracia").value),
      });
      await render(container);
      toast("Configuración de periodos guardada.");
    } catch (err) { toast(err.message, "danger"); }
  };

  for (const tr of container.querySelectorAll("tr[data-pid]")) {
    const id = Number(tr.dataset.pid);
    tr.querySelector(".btn-reabrir")?.addEventListener("click", async () => {
      try {
        await api.post(`/periodos/${id}/reabrir`);
        await render(container);
        toast("Periodo reabierto. Ya puedes registrar movimientos en él.");
      } catch (err) { toast(err.message, "danger"); }
    });
    tr.querySelector(".btn-cerrar")?.addEventListener("click", async () => {
      if (!confirmar("¿Cerrar este periodo? Quedará como solo lectura hasta que lo reabras.")) return;
      try {
        await api.post(`/periodos/${id}/cerrar`);
        await render(container);
        toast("Periodo cerrado.");
      } catch (err) { toast(err.message, "danger"); }
    });
  }

  container.querySelector("#in-avatar").onchange = async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;
    try {
      await api.upload("/perfil/avatar", "archivo", file);
      await refrescarUsuario();
      await render(container);
      toast("Foto actualizada.");
    } catch (err) { toast(err.message, "danger"); }
  };

  container.querySelector("#btn-quitar-avatar")?.addEventListener("click", async () => {
    try {
      await api.del("/perfil/avatar");
      await refrescarUsuario();
      await render(container);
      toast("Foto eliminada.");
    } catch (err) { toast(err.message, "danger"); }
  });

  container.querySelector("#form-perfil").onsubmit = async (e) => {
    e.preventDefault();
    try {
      await api.put("/perfil", { email: container.querySelector("#in-email").value });
      await refrescarUsuario();
      await render(container);
      toast("Perfil actualizado.");
    } catch (err) { toast(err.message, "danger"); }
  };

  container.querySelector("#form-password").onsubmit = async (e) => {
    e.preventDefault();
    try {
      await api.put("/perfil/password", {
        actual: container.querySelector("#in-actual").value,
        nueva: container.querySelector("#in-nueva").value,
      });
      e.target.reset();
      toast("Contraseña cambiada.");
    } catch (err) { toast(err.message, "danger"); }
  };

  container.querySelector("#btn-2fa-on")?.addEventListener("click", () => activar2fa(container));
  container.querySelector("#btn-2fa-off")?.addEventListener("click", () => desactivar2fa(container));
}

async function activar2fa(container) {
  let alta;
  try {
    alta = await api.post("/perfil/2fa/iniciar");
  } catch (err) { toast(err.message, "danger"); return; }

  const body = openModal("Activar verificación en dos pasos", `
    <ol class="ps-3 mb-3" style="font-size:.875rem">
      <li class="mb-2">Escanea este código con tu app de autenticación.</li>
      <li>Escribe el código de 6 dígitos que aparece en la app.</li>
    </ol>
    <div class="text-center mb-3">
      <div class="qr-box"><img src="${alta.qr}" alt="Código QR"></div>
    </div>
    <details class="mb-3">
      <summary class="text-muted-app" style="font-size:.8125rem;cursor:pointer">¿No puedes escanear?</summary>
      <div class="secret-text mt-2">${esc(alta.secret)}</div>
    </details>
    <input type="text" class="form-control codigo-input mb-3" id="f-codigo" inputmode="numeric" maxlength="6" placeholder="000000">
    <button class="btn btn-primary w-100" id="f-guardar">Activar</button>`);

  body.querySelector("#f-guardar").onclick = async () => {
    try {
      await api.post("/perfil/2fa/confirmar", { codigo: body.querySelector("#f-codigo").value });
      closeModal();
      await render(container);
      toast("Verificación en dos pasos activada.");
    } catch (err) { toast(err.message, "danger"); }
  };
}

function desactivar2fa(container) {
  const body = openModal("Desactivar verificación en dos pasos", `
    <p class="text-muted-app" style="font-size:.875rem">
      Tu cuenta quedará protegida solo por la contraseña. Confirma con tu contraseña y un código vigente.
    </p>
    <div class="mb-3">
      <label class="form-label" for="f-pass">Contraseña</label>
      <input type="password" class="form-control" id="f-pass" autocomplete="current-password">
    </div>
    <div class="mb-3">
      <label class="form-label" for="f-codigo">Código de la app</label>
      <input type="text" class="form-control codigo-input" id="f-codigo" inputmode="numeric" maxlength="6" placeholder="000000">
    </div>
    <button class="btn btn-primary w-100" id="f-guardar">Desactivar</button>`);

  body.querySelector("#f-guardar").onclick = async () => {
    if (!confirmar("¿Seguro que quieres desactivar la verificación en dos pasos?")) return;
    try {
      await api.post("/perfil/2fa/desactivar", {
        password: body.querySelector("#f-pass").value,
        codigo: body.querySelector("#f-codigo").value,
      });
      closeModal();
      await render(container);
      toast("Verificación en dos pasos desactivada.", "warning");
    } catch (err) { toast(err.message, "danger"); }
  };
}
