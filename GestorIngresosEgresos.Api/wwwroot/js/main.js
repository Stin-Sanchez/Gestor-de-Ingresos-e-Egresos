import { api, ApiError } from "./api.js";
import { initTema } from "./tema.js";
import { setUsuario } from "./sesion.js";
import * as periodoView from "./periodo.js";
import * as presupuestosView from "./presupuestos.js";
import * as deudasView from "./deudas.js";
import * as ajustesView from "./ajustes.js";

const views = {
  periodo: periodoView,
  presupuestos: presupuestosView,
  deudas: deudasView,
  ajustes: ajustesView,
};
let vistaActual = null;

async function boot() {
  // Al cambiar de tema hay que repintar: Chart.js dibuja en canvas y no hereda CSS.
  initTema(() => vistaActual && navigate(vistaActual));
  try {
    mostrarApp(await api.get("/auth/me"));
  } catch {
    mostrarAcceso();
  }
}

// ── Navegacion ──────────────────────────────────────────────────────────
function mostrarAcceso() {
  document.getElementById("login-view").classList.remove("d-none");
  document.getElementById("app-view").classList.add("d-none");
  paso2fa(false);
}

function mostrarApp(usuario) {
  setUsuario(usuario);
  document.getElementById("login-view").classList.add("d-none");
  document.getElementById("app-view").classList.remove("d-none");
  navigate(location.hash.slice(1) || "periodo");
}

async function navigate(name) {
  if (!views[name]) name = "periodo";
  vistaActual = name;
  location.hash = name;

  for (const link of document.querySelectorAll("[data-view]"))
    link.classList.toggle("active", link.dataset.view === name);
  for (const section of document.querySelectorAll(".view"))
    section.classList.add("d-none");

  bootstrap.Offcanvas.getInstance(document.getElementById("mobile-nav"))?.hide();

  const section = document.getElementById(`view-${name}`);
  section.classList.remove("d-none");
  section.innerHTML = `<div class="empty-state"><i class="bi bi-hourglass"></i> Cargando…</div>`;
  try {
    await views[name].render(section);
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) { mostrarAcceso(); return; }
    section.innerHTML = `<div class="surface empty-state">
        <i class="bi bi-exclamation-triangle d-block fs-4 mb-2"></i>No se pudo cargar esta sección.
        <div class="mt-1" style="font-size:.8125rem">${err.message}</div>
      </div>`;
  }
}

// ── Acceso ──────────────────────────────────────────────────────────────
const error = document.getElementById("auth-error");

function mostrarError(msg) {
  error.textContent = msg;
  error.classList.remove("d-none");
}

function limpiarError() {
  error.classList.add("d-none");
}

function paso2fa(activo) {
  document.getElementById("auth-credenciales").classList.toggle("d-none", activo);
  document.getElementById("auth-2fa").classList.toggle("d-none", !activo);
  if (activo) document.getElementById("in-codigo").focus();
  else document.getElementById("in-codigo").value = "";
}

for (const btn of document.querySelectorAll(".seg-btn")) {
  btn.onclick = () => {
    limpiarError();
    for (const b of document.querySelectorAll(".seg-btn")) b.classList.toggle("active", b === btn);
    document.getElementById("form-entrar").classList.toggle("d-none", btn.dataset.modo !== "entrar");
    document.getElementById("form-registro").classList.toggle("d-none", btn.dataset.modo !== "registro");
  };
}

document.getElementById("form-entrar").onsubmit = async (e) => {
  e.preventDefault();
  limpiarError();
  try {
    const r = await api.post("/auth/login", {
      username: document.getElementById("in-usuario").value,
      password: document.getElementById("in-password").value,
    });
    if (r.requiere2fa) paso2fa(true);
    else mostrarApp(r);
  } catch (err) {
    mostrarError(err instanceof ApiError && err.status === 401
      ? "Usuario o contraseña incorrectos."
      : "No se pudo iniciar sesión.");
  }
};

document.getElementById("form-registro").onsubmit = async (e) => {
  e.preventDefault();
  limpiarError();
  try {
    mostrarApp(await api.post("/auth/registro", {
      username: document.getElementById("re-usuario").value,
      password: document.getElementById("re-password").value,
      email: document.getElementById("re-email").value || null,
    }));
  } catch (err) {
    mostrarError(err.message);
  }
};

document.getElementById("form-2fa").onsubmit = async (e) => {
  e.preventDefault();
  limpiarError();
  try {
    mostrarApp(await api.post("/auth/login/2fa", { codigo: document.getElementById("in-codigo").value }));
  } catch (err) {
    mostrarError(err.message || "Código incorrecto.");
  }
};

document.getElementById("btn-cancelar-2fa").onclick = async () => {
  // La sesion a medias sigue viva hasta que caduca; cerrarla evita dejarla colgando.
  try { await api.post("/auth/logout"); } catch { /* la cookie pudo haber caducado sola */ }
  limpiarError();
  paso2fa(false);
};

for (const btn of document.querySelectorAll("[data-toggle-pass]")) {
  btn.onclick = () => {
    const input = document.getElementById(btn.dataset.togglePass);
    const visible = input.type === "text";
    input.type = visible ? "password" : "text";
    btn.querySelector("i").className = visible ? "bi bi-eye" : "bi bi-eye-slash";
  };
}

async function logout() {
  await api.post("/auth/logout");
  mostrarAcceso();
}
document.getElementById("btn-logout").onclick = logout;
document.getElementById("btn-logout-mobile").onclick = logout;

for (const link of document.querySelectorAll("[data-view]"))
  link.addEventListener("click", (e) => { e.preventDefault(); navigate(link.dataset.view); });

window.addEventListener("hashchange", () => navigate(location.hash.slice(1)));

boot();
