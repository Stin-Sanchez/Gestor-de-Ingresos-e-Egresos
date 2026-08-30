

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

Crea un `.env` junto al `docker-compose.yml`:

```env
MYSQL_ROOT_PASSWORD=cambia-esto
TS_HOSTNAME=gestor
TS_AUTHKEY=tskey-auth-...   # https://login.tailscale.com/admin/settings/keys
```

```bash
docker compose up -d --build
```

Queda disponible en:

* `https://gestor.<tu-tailnet>.ts.net` — a través de Tailscale, con HTTPS automático. La app entra a la tailnet como **nodo propio** (contenedor sidecar `tailscale`), para no chocar con el puerto 443 que pueda estar usando otra app del mismo server.
* `http://<ip-del-server>:8081` — acceso directo dentro de la LAN.

Usuario inicial: `admin` / `admin123` (creado por `migration_v2.sql`), o crea el tuyo desde la pantalla de acceso.

> Para que el HTTPS funcione, la tailnet necesita MagicDNS y certificados HTTPS habilitados en la consola de Tailscale.

### Si pierdes el segundo factor

El 2FA no tiene códigos de respaldo. Si pierdes el teléfono, desactívalo desde la base:

```bash
docker compose exec mysql mysql -uroot -p GestorIngresosDB \
  -e "UPDATE usuarios SET totp_activo = 0, totp_secret = NULL WHERE username = 'TU_USUARIO';"
```

## 🗃️ Migraciones

Los scripts de esquema viven en `docs/sql/` y deben aplicarse en orden sobre `GestorIngresosDB` (para la versión web, `docker-compose.yml` ya las aplica automáticamente al crear el contenedor de MySQL):

1. `migration.sql`
2. `migration_v2.sql`
3. `migration_v3.sql` — agrega la tabla `presupuestos`; requerida para la funcion de presupuestos y tambien para registrar gastos, ya que el guardado de gastos consulta esa tabla.
4. `migration_v4.sql` — agrega `usuario_id` a `periodos` y `deudas` para aislar los datos entre usuarios (necesario solo para la versión web multiusuario).
5. `migration_v5.sql` — agrega correo, avatar y segundo factor TOTP a `usuarios`.

---
*Hecho para mantener las finanzas claras, sin complicaciones.*

