# GestorIngresosEgresos.Api

Versión web del gestor de finanzas personales (migrada desde la app WinForms
`GestorIngresosEgresos/`). Backend ASP.NET Core 9 (minimal API) + frontend
vanilla JS/Bootstrap 5/Chart.js servido como archivos estáticos desde
`wwwroot/`, sin build step.

## Arquitectura

```
Browser (wwwroot/: index.html + js/*.js)
      │ fetch() JSON, cookie de sesión HttpOnly
      ▼
Program.cs (minimal API) ──► Services/*.cs (logica de negocio) ──► Repository/*.cs (ADO.NET) ──► MySQL
```

- **Modelo/**: entidades planas, portadas casi sin cambios desde la app de escritorio.
- **Repository/**: acceso a datos con `MySql.Data` puro (sin ORM), una conexión nueva por llamada (no el singleton que usaba WinForms). Todas las consultas filtran por `usuario_id`, vía columna directa (`periodos`, `deudas`) o `JOIN` a través de `periodo_id`/`gasto_id` para las tablas que cuelgan de esas dos.
- **Services/**: la lógica de negocio y validaciones que antes vivían en `Controller/*.cs` de WinForms (autoprovisión de periodos, reglas de los "sobres", transacción de abono a deuda, etc.), sin cambios de comportamiento.
- **Auth**: cookie de sesión (`CookieAuthenticationDefaults`), no JWT. Login verifica primero con BCrypt; si el usuario todavía tiene el hash SHA-256 de la app de escritorio, lo valida con ese algoritmo y lo re-hashea a BCrypt automáticamente (sin invalidar cuentas existentes).

## Configuración

Connection string en `appsettings.json` (`ConnectionStrings:Default`), sobreescribible con la variable de entorno `ConnectionStrings__Default` (así se hace en `docker-compose.yml`).

## Correr en local

```bash
dotnet run
```

Necesita una base MySQL con el esquema de `docs/sql/migration.sql` → `migration_v2.sql` → `migration_v3.sql` → `migration_v4.sql` (en ese orden) ya aplicado.

## Self-check de reglas de negocio

La lógica de umbrales/bloqueo de los "sobres" (`Modelo/PresupuestoResumen.cs`) no tiene proyecto de tests aparte; se verifica con:

```bash
dotnet run -- --selftest
```

## Endpoints

Todos bajo `/api`, todos requieren sesión excepto `POST /auth/login`.

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/auth/login` | Inicia sesión, setea cookie |
| POST | `/auth/logout` | Cierra sesión |
| GET | `/auth/me` | Usuario autenticado |
| GET | `/periodos` | Lista periodos del usuario |
| GET | `/periodos/actual?anio=&mes=` | Periodo del mes (lo crea si es el mes actual y no existe) |
| GET | `/periodos/{id}` | Periodo por id |
| PUT | `/periodos/{id}/sueldo` | Actualiza sueldo base |
| POST | `/periodos/{id}/cerrar` | Cierra el periodo |
| GET/POST | `/periodos/{id}/ingresos` | Ingresos del periodo |
| PUT/DELETE | `/ingresos/{id}` | Editar/eliminar ingreso |
| GET/POST | `/periodos/{id}/gastos` | Gastos del periodo |
| PUT/DELETE | `/gastos/{id}` | Editar/eliminar gasto |
| GET | `/categorias` | Catálogo de categorías |
| GET | `/periodos/{id}/sobres` | Resumen de sobres del periodo |
| GET | `/gastos/{id}/resumen` | Resumen de un sobre puntual |
| GET/POST | `/gastos/{id}/consumos` | Consumos de un sobre |
| PUT/DELETE | `/consumos/{id}` | Editar/eliminar consumo |
| GET/POST | `/deudas` | Listar/crear deudas |
| GET | `/deudas/activas` | Solo deudas activas |
| GET | `/deudas/total-pendiente` | Suma de saldo pendiente |
| DELETE | `/deudas/{id}` | Eliminar deuda |
| GET | `/deudas/{id}/abonos` | Historial de abonos |
| POST | `/deudas/{id}/abonos` | Registrar abono (transaccional) |

## Despliegue (homelab)

Ver `docker-compose.yml` en la raíz del repo — levanta la API + MySQL en contenedores.
