// Un solo modal Bootstrap reutilizado por todas las vistas para formularios de alta/edicion.
let instance = null;

export function openModal(title, bodyHtml) {
  document.getElementById("app-modal-title").textContent = title;
  document.getElementById("app-modal-body").innerHTML = bodyHtml;
  instance ??= new bootstrap.Modal(document.getElementById("app-modal"));
  instance.show();
  return document.getElementById("app-modal-body");
}

export function closeModal() {
  instance?.hide();
}
