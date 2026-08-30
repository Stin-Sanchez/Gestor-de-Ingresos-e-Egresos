import { api } from "./api.js";
import { esc, toast, confirmar } from "./ui.js";
import { openModal, closeModal } from "./modal.js";
import { avatarHtml, refrescarUsuario } from "./sesion.js";

let perfil = null;

export async function render(container) {
  perfil = await api.get("/perfil");
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

function bind(container) {
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
