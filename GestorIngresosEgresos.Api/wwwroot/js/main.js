import { api, ApiError } from "./api.js";
import { initTema } from "./tema.js";
import * as periodoView from "./periodo.js";
import * as presupuestosView from "./presupuestos.js";
import * as deudasView from "./deudas.js";

const views = { periodo: periodoView, presupuestos: presupuestosView, deudas: deudasView };
let vistaActual = null;

async function boot() {
  // Al cambiar de tema hay que repintar: Chart.js dibuja en canvas y no hereda CSS.
  initTema(() => vistaActual && navigate(vistaActual));
  try {
    showApp(await api.get("/auth/me"));
  } catch {
    showLogin();
  }
}

function showLogin() {
  document.getElementById("login-view").classList.remove("d-none");
  document.getElementById("app-view").classList.add("d-none");
}

function showApp(me) {
  document.getElementById("login-view").classList.add("d-none");
  document.getElementById("app-view").classList.remove("d-none");
  document.getElementById("nav-username").textContent = me.username;
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
  await views[name].render(section);
}

document.getElementById("login-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  const errorBox = document.getElementById("login-error");
  errorBox.classList.add("d-none");
  try {
    const me = await api.post("/auth/login", {
      username: document.getElementById("login-username").value,
      password: document.getElementById("login-password").value,
    });
    showApp(me);
  } catch (err) {
    errorBox.textContent = err instanceof ApiError && err.status === 401
      ? "Usuario o contraseña incorrectos."
      : "No se pudo iniciar sesión.";
    errorBox.classList.remove("d-none");
  }
});

document.getElementById("toggle-password").addEventListener("click", (e) => {
  const input = document.getElementById("login-password");
  const visible = input.type === "text";
  input.type = visible ? "password" : "text";
  e.currentTarget.querySelector("i").className = visible ? "bi bi-eye" : "bi bi-eye-slash";
});

async function logout() {
  await api.post("/auth/logout");
  showLogin();
}
document.getElementById("btn-logout").addEventListener("click", logout);
document.getElementById("btn-logout-mobile").addEventListener("click", logout);

for (const link of document.querySelectorAll("[data-view]"))
  link.addEventListener("click", (e) => { e.preventDefault(); navigate(link.dataset.view); });

window.addEventListener("hashchange", () => navigate(location.hash.slice(1)));

boot();
