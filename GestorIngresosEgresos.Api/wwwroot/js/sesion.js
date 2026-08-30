import { api } from "./api.js";
import { esc } from "./ui.js";

let usuario = null;

export function usuarioActual() {
  return usuario;
}

export function setUsuario(u) {
  usuario = u;
  pintarBarraLateral();
}

// Tras cambiar avatar o correo hay que releer el perfil para que la barra lateral no quede vieja.
export async function refrescarUsuario() {
  setUsuario(await api.get("/perfil"));
}

export function avatarHtml(u, clase = "avatar-sm") {
  // El nombre del archivo va como parametro para que el navegador no sirva el avatar
  // anterior desde cache cuando el usuario sube uno nuevo.
  return u?.avatar
    ? `<span class="avatar ${clase}"><img src="/api/perfil/avatar?v=${encodeURIComponent(u.avatar)}" alt=""></span>`
    : `<span class="avatar ${clase}">${esc((u?.username ?? "?").charAt(0).toUpperCase())}</span>`;
}

function pintarBarraLateral() {
  if (!usuario) return;
  document.getElementById("nav-username").textContent = usuario.username;
  document.getElementById("nav-avatar").innerHTML = avatarHtml(usuario);
}
