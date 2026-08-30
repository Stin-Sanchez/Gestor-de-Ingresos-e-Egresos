// Modo claro/oscuro sobre data-bs-theme (nativo de Bootstrap 5.3). El valor inicial
// lo fija un script inline en index.html para evitar el flash al cargar; aqui solo
// se sincroniza el icono y se alterna.
const KEY = "tema";

export function esOscuro() {
  return document.documentElement.getAttribute("data-bs-theme") === "dark";
}

function pintarIconos() {
  const clase = esOscuro() ? "bi bi-sun" : "bi bi-moon-stars";
  for (const btn of document.querySelectorAll(".btn-tema i")) btn.className = clase;
}

export function initTema(onChange) {
  pintarIconos();

  for (const btn of document.querySelectorAll(".btn-tema")) {
    btn.addEventListener("click", () => {
      const oscuro = !esOscuro();
      document.documentElement.setAttribute("data-bs-theme", oscuro ? "dark" : "light");
      localStorage.setItem(KEY, oscuro ? "dark" : "light");
      pintarIconos();
      onChange?.();
    });
  }

  // Sigue al sistema mientras el usuario no haya elegido un tema explicitamente.
  matchMedia("(prefers-color-scheme: dark)").addEventListener("change", (e) => {
    if (localStorage.getItem(KEY)) return;
    document.documentElement.setAttribute("data-bs-theme", e.matches ? "dark" : "light");
    pintarIconos();
    onChange?.();
  });
}
