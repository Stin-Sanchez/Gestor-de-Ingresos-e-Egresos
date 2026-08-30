

Una herramienta sencilla y funcional para la gestión de finanzas personales. Diseñada para operar de manera **totalmente independiente**, esta aplicación funciona como una libreta digital inteligente donde tú tienes el control absoluto de los datos que ingresas.

> **Nota:** Esta aplicación **NO** se conecta a cuentas bancarias ni servicios externos. Es un entorno seguro y privado para llevar tus registros manualmente.

## 💡 ¿Por qué usar esta app?

* **100% Privada:** Al no requerir conexión con bancos, tus credenciales financieras nunca están en riesgo.
* **Simplicidad:** Sin gráficos complejos ni funciones innecesarias. Solo lo que necesitas saber: cuánto entra y cuánto sale.
* **Control Manual:** Ideal para quienes prefieren registrar cada movimiento por sí mismos para ser más conscientes de sus gastos.

## 🚀 Funcionalidades

* ✅ **Registro Rápido:** Agrega ingresos o egresos en segundos.
* ✅ **Balance al Instante:** Visualiza la diferencia entre lo que ganas y lo que gastas automáticamente.
* ✅ **Historial Limpio:** Revisa tus movimientos pasados en una lista clara y ordenada.
* ✅ **Categorización Básica:** Etiqueta tus gastos (Comida, Casa, Ocio) para saber a dónde va tu dinero.

## 🛠️ Tecnologías

Este repo tiene dos versiones de la misma app:

* **`GestorIngresosEgresos/`** — la app de escritorio original (C# / WinForms).
* **`GestorIngresosEgresos.Api/`** — la migración a web (ASP.NET Core 9 minimal API + frontend vanilla JS/Bootstrap 5/Chart.js), pensada para correr en un homelab vía Docker. Ver `GestorIngresosEgresos.Api/README.md` para arquitectura y endpoints.

Ambas comparten la misma base de datos MySQL.

    <img width="1920" height="1040" alt="image" src="https://github.com/user-attachments/assets/ff88b00c-64dd-4ddc-b365-5cadb530304a" />

## 🐳 Correr la versión web (Docker)

```bash
docker compose up -d --build
```

Levanta MySQL + la API en `http://localhost:8080` (sirve también el frontend). Usuario inicial: `admin` / `admin123` (creado por `migration_v2.sql`).

## 🗃️ Migraciones

Los scripts de esquema viven en `docs/sql/` y deben aplicarse en orden sobre `GestorIngresosDB` (para la versión web, `docker-compose.yml` ya las aplica automáticamente al crear el contenedor de MySQL):

1. `migration.sql`
2. `migration_v2.sql`
3. `migration_v3.sql` — agrega la tabla `presupuestos`; requerida para la funcion de presupuestos y tambien para registrar gastos, ya que el guardado de gastos consulta esa tabla.
4. `migration_v4.sql` — agrega `usuario_id` a `periodos` y `deudas` para aislar los datos entre usuarios (necesario solo para la versión web multiusuario).

---
*Hecho para mantener las finanzas claras, sin complicaciones.*

