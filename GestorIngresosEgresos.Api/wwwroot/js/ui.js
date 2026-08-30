// Helpers de UI compartidos entre vistas: formateo, escape y toasts.
export function money(n) {
  return new Intl.NumberFormat("es-MX", { style: "currency", currency: "USD" }).format(n ?? 0);
}

export function dateInputValue(d) {
  const date = d ? new Date(d) : new Date();
  return date.toISOString().slice(0, 10);
}

export function fechaCorta(iso) {
  return new Date(iso).toLocaleDateString("es-ES", { day: "2-digit", month: "short" });
}

// Las descripciones las escribe el usuario y se inyectan con innerHTML: escapar
// evita que un "<img onerror=...>" en una descripcion se ejecute al renderizar.
export function esc(s) {
  return String(s ?? "").replace(/[&<>"']/g, c =>
    ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
}

export function toast(message, variant = "success") {
  const icon = { success: "bi-check-circle", danger: "bi-exclamation-circle", warning: "bi-exclamation-triangle" }[variant];
  const color = { success: "var(--app-pos)", danger: "var(--app-neg)", warning: "#d9a406" }[variant];

  const el = document.createElement("div");
  el.className = "toast align-items-center border-0 surface";
  el.setAttribute("role", "alert");
  el.innerHTML = `<div class="d-flex align-items-center">
      <div class="toast-body d-flex align-items-center gap-2" style="font-size:.8125rem">
        <i class="bi ${icon}" style="color:${color}"></i><span>${esc(message)}</span>
      </div>
      <button type="button" class="btn-close btn-close-sm me-2 m-auto" data-bs-dismiss="toast"></button>
    </div>`;
  document.getElementById("toast-container").appendChild(el);

  const t = new bootstrap.Toast(el, { delay: 4000 });
  t.show();
  el.addEventListener("hidden.bs.toast", () => el.remove());
}

export function confirmar(mensaje) {
  return window.confirm(mensaje);
}

export const ESTADO_LABEL = { OK: "En curso", ALERTA: "Mitad", CRITICO: "Por agotarse", EXCEDIDO: "Agotado" };
