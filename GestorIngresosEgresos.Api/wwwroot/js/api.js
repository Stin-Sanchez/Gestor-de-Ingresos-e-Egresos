// Wrapper delgado sobre fetch: JSON en ambos sentidos, cookie de sesion, errores tipados.
export class ApiError extends Error {
  constructor(status, message) {
    super(message);
    this.status = status;
  }
}

async function request(method, path, body) {
  const res = await fetch(`/api${path}`, {
    method,
    headers: body ? { "Content-Type": "application/json" } : undefined,
    body: body ? JSON.stringify(body) : undefined,
    credentials: "same-origin",
  });

  if (res.status === 204) return null;

  const isJson = res.headers.get("content-type")?.includes("application/json");
  const data = isJson ? await res.json() : null;

  if (!res.ok) throw new ApiError(res.status, data?.error ?? `Error ${res.status}`);
  return data;
}

// Los archivos van como multipart, asi que no pasan por request(), que serializa JSON.
async function upload(path, campo, file) {
  const form = new FormData();
  form.append(campo, file);

  const res = await fetch(`/api${path}`, { method: "POST", body: form, credentials: "same-origin" });
  const data = res.headers.get("content-type")?.includes("application/json") ? await res.json() : null;
  if (!res.ok) throw new ApiError(res.status, data?.error ?? `Error ${res.status}`);
  return data;
}

export const api = {
  get: (path) => request("GET", path),
  post: (path, body) => request("POST", path, body ?? {}),
  put: (path, body) => request("PUT", path, body ?? {}),
  del: (path) => request("DELETE", path),
  upload,
};
