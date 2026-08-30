// Helpers de UI compartidos entre vistas: formateo y toasts de Bootstrap.
export function money(n) {
  return new Intl.NumberFormat("es-MX", { style: "currency", currency: "USD" }).format(n ?? 0);
}

export function dateInputValue(d) {
  const date = d ? new Date(d) : new Date();
  return date.toISOString().slice(0, 10);
}

export function toast(message, variant = "primary") {
  const container = document.getElementById("toast-container");
  const el = document.createElement("div");
  el.className = `toast align-items-center text-bg-${variant} border-0`;
  el.setAttribute("role", "alert");
  el.innerHTML = `<div class="d-flex">
      <div class="toast-body">${message}</div>
      <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
    </div>`;
  container.appendChild(el);
  const t = new bootstrap.Toast(el, { delay: 4000 });
  t.show();
  el.addEventListener("hidden.bs.toast", () => el.remove());
}

export function confirmar(mensaje) {
  return window.confirm(mensaje);
}

export const ESTADO_LABEL = { OK: "OK", ALERTA: "Alerta", CRITICO: "Crítico", EXCEDIDO: "Excedido" };
