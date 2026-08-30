

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
```

```bash
docker compose up -d --build
```

Queda en `http://<ip-del-server>:8081`. Usuario inicial: `admin` / `admin123` (creado por `migration_v2.sql`), o crea el tuyo desde la pantalla de acceso.

### HTTPS con Tailscale

Usando el Tailscale que ya corre en el host (no hace falta ningún contenedor extra):

```bash
sudo tailscale serve --bg --https=8443 http://127.0.0.1:8081
tailscale serve status
```

Queda en `https://<hostname>.<tu-tailnet>.ts.net:8443`, con certificado automático.

Se usa un puerto distinto de 443 porque un nodo de Tailscale tiene un solo hostname
MagicDNS, y el 443 de este server puede estar ya ocupado por otra app. Si prefieres una
URL sin puerto, `--https=443` sirve siempre que nada más lo esté usando.

> Requiere MagicDNS y certificados HTTPS habilitados en la consola de Tailscale.

### Aplicar una migración sobre una base que ya existe

`docker-compose.yml` solo corre las migraciones cuando crea la base por primera vez.
Si ya tenías datos, aplícala a mano:

```bash
docker compose exec -T mysql sh -c 'mysql -uroot -p"$MYSQL_ROOT_PASSWORD" GestorIngresosDB' < docs/sql/migration_v5.sql
```

La contraseña se toma de la variable que ya vive dentro del contenedor. No uses
`-p` a secas con un archivo redirigido: sin TTY, `mysql` lee la contraseña de stdin
y se come la primera línea del script, con lo que falla con *access denied*.

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
6. `migration_v6.sql` — deudas en dos direcciones (`tipo` en `deudas`, `deuda_id` en `ingresos`).

---
*Hecho para mantener las finanzas claras, sin complicaciones.*

