# Presupuestos por categoría — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let the user assign a monthly budget per expense category, track consumption live from existing `gastos`, block expenses that would exceed the budget, and show a threshold-based notice (50/80/100%) after saving — with a new "Presupuestos" screen in the sidebar showing progress cards.

**Architecture:** Standard 3-layer pattern already used throughout the app (`Modelo` → `Repository` → `Controller` → `Vista`), backed by MySQL. A new `presupuestos` table stores only the assigned limit per period+category; "spent" is always computed live by summing `gastos`, never stored. `GastoController` gains budget-aware validation (block on overspend) and a threshold-notice calculation, wired into the existing `FormPeriodo` gasto flow. A new `FormPresupuestos` screen (reachable from the sidebar) lists budget cards per category with a colored progress bar.

**Tech Stack:** C# / .NET Framework 4.8, WinForms (hand-built UI in code, no designer files), MySQL via `MySql.Data`, old-style `.csproj` (files must be registered explicitly in `<Compile Include>`).

## Global Constraints

- Budgets are defined **per category and per period (month)** — spec: [docs/superpowers/specs/2026-08-20-presupuestos-por-categoria-design.md](../specs/2026-08-20-presupuestos-por-categoria-design.md).
- A gasto that would exceed its category's available budget must be **blocked** (not just warned) — `throw new ArgumentException(...)`, shown via the existing `MessageBox` error pattern.
- After a successful gasto save/update in a budgeted category, show an informational notice when consumption is ≥50%, ≥80%, or ≥100% (no "crossing" tracking needed — just evaluate the resulting percentage each time).
- No automated test framework is introduced (explicit spec decision). The one exception is a tiny in-repo self-check (`PresupuestoResumen.SelfCheck()`, run via `GestorIngresosEgresos.exe --selftest`) for the pure percentage/threshold math, since that's the one piece of non-trivial branching logic with no DB dependency.
- All budget CRUD actions are disabled when the period is `CERRADO`, mirroring existing Ingreso/Gasto behavior.
- This is an old-style `.csproj` — every new `.cs` file must be added to `GestorIngresosEgresos/GestorIngresosEgresos.csproj` under the matching `<ItemGroup>` comment section, or it will not compile into the build.
- Build verification command (confirmed working in this environment — plain `msbuild.exe` fails on this machine's toolchain):
  ```bash
  cd "D:/Proyectos_Personales/Proyectos/Gestor-de-Ingresos-e-Egresos" && MSYS_NO_PATHCONV=1 "/c/Program Files/dotnet/dotnet.exe" msbuild GestorIngresosEgresos/GestorIngresosEgresos.csproj -p:Configuration=Debug -p:GenerateResourceMSBuildRuntime=CurrentRuntime -p:GenerateResourceMSBuildArchitecture=CurrentArchitecture -nologo -v:minimal
  ```
  Expected on success: a single line `GestorIngresosEgresos -> ...\bin\Debug\GestorIngresosEgresos.exe` and nothing else.

---

### Task 1: Migración SQL — tabla `presupuestos`

**Files:**
- Create: `docs/sql/migration_v3.sql`

**Interfaces:**
- Produces: table `presupuestos(id, periodo_id, categoria_id, monto)`, unique on `(periodo_id, categoria_id)`, FKs to `periodos(id)` and `categorias_gasto(id)` — consumed by `PresupuestoRepository` in Task 3.

- [ ] **Step 1: Write the migration file**

```sql
-- ============================================================
-- Gestor Financiero Personal — Schema v3
-- Presupuestos por categoria y periodo
-- Aplicar sobre GestorIngresosDB (schema v2 ya debe existir)
-- ============================================================

USE GestorIngresosDB;

CREATE TABLE presupuestos (
    id           INT AUTO_INCREMENT PRIMARY KEY,
    periodo_id   INT           NOT NULL,
    categoria_id INT           NOT NULL,
    monto        DECIMAL(15,2) NOT NULL,
    UNIQUE KEY uq_periodo_categoria (periodo_id, categoria_id),
    CONSTRAINT fk_pre_periodo   FOREIGN KEY (periodo_id)   REFERENCES periodos(id)         ON DELETE CASCADE,
    CONSTRAINT fk_pre_categoria FOREIGN KEY (categoria_id) REFERENCES categorias_gasto(id) ON DELETE CASCADE
);
```

- [ ] **Step 2: Verify by inspection**

Confirm column/table naming matches `docs/sql/migration_v2.sql` conventions (snake_case, `DECIMAL(15,2)`, explicit `CONSTRAINT fk_..._...` names) and that both FK targets (`periodos`, `categorias_gasto`) already exist in `migration_v2.sql`. No DB connection needed for this step — the file is applied manually by the user against their MySQL instance later (same as the two existing migration files, which nothing in the app executes automatically).

- [ ] **Step 3: Commit**

```bash
git add docs/sql/migration_v3.sql
git commit -m "Add presupuestos table migration for per-category monthly budgets"
```

---

### Task 2: Modelo `Presupuesto` / `PresupuestoResumen` + self-check

**Files:**
- Create: `GestorIngresosEgresos/Modelo/Presupuesto.cs`
- Create: `GestorIngresosEgresos/Modelo/PresupuestoResumen.cs`
- Modify: `GestorIngresosEgresos/GestorIngresosEgresos.csproj` (register both files)
- Modify: `GestorIngresosEgresos/Program.cs` (add `--selftest` entry point)

**Interfaces:**
- Produces: `Presupuesto { int Id, int PeriodoId, int CategoriaId, decimal Monto }`.
- Produces: `PresupuestoResumen { int Id, int CategoriaId, string CategoriaNombre, decimal Limite, decimal Gastado, decimal Disponible (computed), decimal Porcentaje (computed), EstadoPresupuesto Estado (computed) }` and `enum EstadoPresupuesto { OK, ALERTA, CRITICO, EXCEDIDO }` — consumed by `PresupuestoRepository`/`PresupuestoController` (Tasks 3–4) and `FormPresupuestos`/`FormPresupuestoDialog` (Tasks 6–7).
- Produces: `PresupuestoResumen.SelfCheck()` static method returning `bool` — this task's own verification.

- [ ] **Step 1: Create `Presupuesto.cs`**

```csharp
namespace GestorIngresosEgresos.Modelo
{
    public class Presupuesto
    {
        public int Id { get; set; }
        public int PeriodoId { get; set; }
        public int CategoriaId { get; set; }
        public decimal Monto { get; set; }
    }
}
```

- [ ] **Step 2: Create `PresupuestoResumen.cs` with the self-check**

```csharp
using System;

namespace GestorIngresosEgresos.Modelo
{
    public enum EstadoPresupuesto { OK, ALERTA, CRITICO, EXCEDIDO }

    public class PresupuestoResumen
    {
        public int Id { get; set; }
        public int CategoriaId { get; set; }
        public string CategoriaNombre { get; set; }
        public decimal Limite { get; set; }
        public decimal Gastado { get; set; }

        public decimal Disponible => Limite - Gastado;

        public decimal Porcentaje => Limite <= 0 ? 0 : Math.Min(Gastado / Limite * 100m, 999m);

        public EstadoPresupuesto Estado =>
            Porcentaje >= 100 ? EstadoPresupuesto.EXCEDIDO :
            Porcentaje >= 80  ? EstadoPresupuesto.CRITICO :
            Porcentaje >= 50  ? EstadoPresupuesto.ALERTA :
                                 EstadoPresupuesto.OK;

        // ponytail: self-check en vez de un proyecto de tests aparte (el proyecto no tiene ninguno);
        // correr con "GestorIngresosEgresos.exe --selftest". Si se agrega logica no trivial nueva a este
        // calculo, agregar mas casos aqui en vez de crear un test project.
        public static bool SelfCheck()
        {
            bool ok = true;
            Action<bool, string> check = (cond, msg) =>
            {
                if (!cond) { Console.WriteLine("FALLO: " + msg); ok = false; }
            };

            var r0 = new PresupuestoResumen { Limite = 20m, Gastado = 0m };
            check(r0.Porcentaje == 0m, "0% cuando no hay gasto");
            check(r0.Estado == EstadoPresupuesto.OK, "estado OK en 0%");
            check(r0.Disponible == 20m, "disponible = limite cuando no hay gasto");

            var r49 = new PresupuestoResumen { Limite = 20m, Gastado = 9.8m };
            check(r49.Porcentaje == 49m, "49% se calcula correctamente");
            check(r49.Estado == EstadoPresupuesto.OK, "49% sigue siendo OK");

            var r50 = new PresupuestoResumen { Limite = 20m, Gastado = 10m };
            check(r50.Porcentaje == 50m, "50% se calcula correctamente");
            check(r50.Estado == EstadoPresupuesto.ALERTA, "50% es ALERTA");

            var r80 = new PresupuestoResumen { Limite = 20m, Gastado = 16m };
            check(r80.Estado == EstadoPresupuesto.CRITICO, "80% es CRITICO");

            var r100 = new PresupuestoResumen { Limite = 20m, Gastado = 20m };
            check(r100.Estado == EstadoPresupuesto.EXCEDIDO, "100% es EXCEDIDO");
            check(r100.Disponible == 0m, "disponible = 0 al 100%");

            var r150 = new PresupuestoResumen { Limite = 20m, Gastado = 30m };
            check(r150.Estado == EstadoPresupuesto.EXCEDIDO, "150% sigue EXCEDIDO");
            check(r150.Disponible == -10m, "disponible negativo cuando se excede");

            var rSinLimite = new PresupuestoResumen { Limite = 0m, Gastado = 5m };
            check(rSinLimite.Porcentaje == 0m, "limite 0 no lanza division por cero, retorna 0%");

            Console.WriteLine(ok ? "OK: todos los checks pasaron." : "Uno o mas checks fallaron.");
            return ok;
        }
    }
}
```

- [ ] **Step 3: Register both files in the `.csproj`**

In `GestorIngresosEgresos/GestorIngresosEgresos.csproj`, find the `<!-- Modelos -->` block:

```xml
    <!-- Modelos -->
    <Compile Include="Modelo\CategoriaGasto.cs" />
    <Compile Include="Modelo\Deuda.cs" />
    <Compile Include="Modelo\Gasto.cs" />
    <Compile Include="Modelo\Ingreso.cs" />
    <Compile Include="Modelo\Periodo.cs" />
    <Compile Include="Modelo\Usuario.cs" />
```

Replace with:

```xml
    <!-- Modelos -->
    <Compile Include="Modelo\CategoriaGasto.cs" />
    <Compile Include="Modelo\Deuda.cs" />
    <Compile Include="Modelo\Gasto.cs" />
    <Compile Include="Modelo\Ingreso.cs" />
    <Compile Include="Modelo\Periodo.cs" />
    <Compile Include="Modelo\Presupuesto.cs" />
    <Compile Include="Modelo\PresupuestoResumen.cs" />
    <Compile Include="Modelo\Usuario.cs" />
```

- [ ] **Step 4: Wire `--selftest` into `Program.cs`**

Replace the full contents of `GestorIngresosEgresos/Program.cs`:

```csharp
using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Vista;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace GestorIngresosEgresos
{
    static class Program
    {
        // Embebido en el assembly via <EmbeddedResource> en .csproj
        public static Icon AppIcon { get; private set; }

        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--selftest")
                return PresupuestoResumen.SelfCheck() ? 0 : 1;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                string icoPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
                AppIcon = new Icon(icoPath);
            }
            catch { }

            using (FormLogin login = new FormLogin())
            {
                if (login.ShowDialog() == DialogResult.OK)
                    Application.Run(new FormDashboard());
            }
            return 0;
        }
    }
}
```

- [ ] **Step 5: Build**

Run the build command from Global Constraints. Expected: success line only, no `error` lines.

- [ ] **Step 6: Run the self-check and verify it passes**

```bash
cd "D:/Proyectos_Personales/Proyectos/Gestor-de-Ingresos-e-Egresos/GestorIngresosEgresos/bin/Debug" && ./GestorIngresosEgresos.exe --selftest; echo "EXIT:$?"
```

Expected: `EXIT:0`. If `EXIT:1`, a check failed — fix `PresupuestoResumen` (not the self-check) and re-run.

- [ ] **Step 7: Commit**

```bash
git add GestorIngresosEgresos/Modelo/Presupuesto.cs GestorIngresosEgresos/Modelo/PresupuestoResumen.cs GestorIngresosEgresos/GestorIngresosEgresos.csproj GestorIngresosEgresos/Program.cs
git commit -m "Add Presupuesto/PresupuestoResumen models with a self-check for the threshold math"
```

---

### Task 3: `PresupuestoRepository`

**Files:**
- Create: `GestorIngresosEgresos/Repository/PresupuestoRepository.cs`
- Modify: `GestorIngresosEgresos/GestorIngresosEgresos.csproj` (register file)

**Interfaces:**
- Consumes: `Presupuesto`, `PresupuestoResumen` (Task 2); `ConexionDB.GetInstance().GetConnection()` (existing, see `GastoRepository.cs`).
- Produces: `PresupuestoRepository` with `List<PresupuestoResumen> ObtenerResumenPorPeriodo(int periodoId)`, `Presupuesto ObtenerPorCategoria(int periodoId, int categoriaId)` (nullable return), `decimal ObtenerGastado(int periodoId, int categoriaId, int? excludeGastoId)`, `Presupuesto Guardar(Presupuesto p)`, `void Actualizar(Presupuesto p)`, `void Eliminar(int id)` — consumed by `PresupuestoController` (Task 4) and `GastoController` (Task 5).

- [ ] **Step 1: Create `PresupuestoRepository.cs`**

```csharp
using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Util;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace GestorIngresosEgresos.Repository
{
    public class PresupuestoRepository
    {
        private readonly MySqlConnection _conn;

        public PresupuestoRepository()
        {
            _conn = ConexionDB.GetInstance().GetConnection();
        }

        public List<PresupuestoResumen> ObtenerResumenPorPeriodo(int periodoId)
        {
            var lista = new List<PresupuestoResumen>();
            string sql = @"SELECT p.id, p.categoria_id, c.nombre AS categoria_nombre, p.monto AS limite,
                                  COALESCE(SUM(g.monto), 0) AS gastado
                           FROM presupuestos p
                           JOIN categorias_gasto c ON c.id = p.categoria_id
                           LEFT JOIN gastos g ON g.periodo_id = p.periodo_id AND g.categoria_id = p.categoria_id AND g.deuda_id IS NULL
                           WHERE p.periodo_id = @pid
                           GROUP BY p.id, p.categoria_id, c.nombre, p.monto
                           ORDER BY c.nombre";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@pid", periodoId);
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        lista.Add(new PresupuestoResumen
                        {
                            Id              = r.GetInt32("id"),
                            CategoriaId     = r.GetInt32("categoria_id"),
                            CategoriaNombre = r.GetString("categoria_nombre"),
                            Limite          = r.GetDecimal("limite"),
                            Gastado         = r.GetDecimal("gastado")
                        });
            }
            return lista;
        }

        public Presupuesto ObtenerPorCategoria(int periodoId, int categoriaId)
        {
            string sql = "SELECT * FROM presupuestos WHERE periodo_id = @pid AND categoria_id = @cat LIMIT 1";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@pid", periodoId);
                cmd.Parameters.AddWithValue("@cat", categoriaId);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new Presupuesto
                    {
                        Id          = r.GetInt32("id"),
                        PeriodoId   = r.GetInt32("periodo_id"),
                        CategoriaId = r.GetInt32("categoria_id"),
                        Monto       = r.GetDecimal("monto")
                    };
                }
            }
        }

        public decimal ObtenerGastado(int periodoId, int categoriaId, int? excludeGastoId)
        {
            string sql = @"SELECT COALESCE(SUM(monto), 0) FROM gastos
                           WHERE periodo_id = @pid AND categoria_id = @cat AND deuda_id IS NULL
                             AND (@exclude IS NULL OR id <> @exclude)";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@pid", periodoId);
                cmd.Parameters.AddWithValue("@cat", categoriaId);
                cmd.Parameters.AddWithValue("@exclude", (object)excludeGastoId ?? DBNull.Value);
                return Convert.ToDecimal(cmd.ExecuteScalar());
            }
        }

        public Presupuesto Guardar(Presupuesto p)
        {
            string sql = @"INSERT INTO presupuestos (periodo_id, categoria_id, monto) VALUES (@pid, @cat, @monto);
                           SELECT LAST_INSERT_ID();";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@pid",   p.PeriodoId);
                cmd.Parameters.AddWithValue("@cat",   p.CategoriaId);
                cmd.Parameters.AddWithValue("@monto", p.Monto);
                p.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
            return p;
        }

        public void Actualizar(Presupuesto p)
        {
            string sql = "UPDATE presupuestos SET monto = @monto WHERE id = @id";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@monto", p.Monto);
                cmd.Parameters.AddWithValue("@id",    p.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public void Eliminar(int id)
        {
            string sql = "DELETE FROM presupuestos WHERE id = @id";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
```

- [ ] **Step 2: Register in the `.csproj`**

Find the `<!-- Repositorios -->` block:

```xml
    <!-- Repositorios -->
    <Compile Include="Repository\CategoriaRepository.cs" />
    <Compile Include="Repository\DeudaRepository.cs" />
    <Compile Include="Repository\GastoRepository.cs" />
    <Compile Include="Repository\IngresoRepository.cs" />
    <Compile Include="Repository\PeriodoRepository.cs" />
    <Compile Include="Repository\Repository.cs" />
    <Compile Include="Repository\UsuarioRepository.cs" />
```

Replace with:

```xml
    <!-- Repositorios -->
    <Compile Include="Repository\CategoriaRepository.cs" />
    <Compile Include="Repository\DeudaRepository.cs" />
    <Compile Include="Repository\GastoRepository.cs" />
    <Compile Include="Repository\IngresoRepository.cs" />
    <Compile Include="Repository\PeriodoRepository.cs" />
    <Compile Include="Repository\PresupuestoRepository.cs" />
    <Compile Include="Repository\Repository.cs" />
    <Compile Include="Repository\UsuarioRepository.cs" />
```

- [ ] **Step 3: Build**

Run the build command from Global Constraints. Expected: success line only.

- [ ] **Step 4: Commit**

```bash
git add GestorIngresosEgresos/Repository/PresupuestoRepository.cs GestorIngresosEgresos/GestorIngresosEgresos.csproj
git commit -m "Add PresupuestoRepository for budget CRUD and live consumption queries"
```

---

### Task 4: `PresupuestoController`

**Files:**
- Create: `GestorIngresosEgresos/Controller/PresupuestoController.cs`
- Modify: `GestorIngresosEgresos/GestorIngresosEgresos.csproj` (register file)

**Interfaces:**
- Consumes: `PresupuestoRepository` (Task 3), `CategoriaRepository` (existing, `Repository/CategoriaRepository.cs`, method `List<CategoriaGasto> ObtenerTodas()`).
- Produces: `PresupuestoController` with `List<PresupuestoResumen> ObtenerResumen(int periodoId)`, `List<CategoriaGasto> ObtenerCategoriasSinPresupuesto(int periodoId)`, `Presupuesto Guardar(Presupuesto p)` (throws `ArgumentException` if `Monto <= 0` or a budget already exists for that category/period), `void Actualizar(Presupuesto p)` (throws if `Monto <= 0`), `void Eliminar(int id)` — consumed by `FormPresupuestos` (Task 7).

- [ ] **Step 1: Create `PresupuestoController.cs`**

```csharp
using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Repository;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GestorIngresosEgresos.Controller
{
    public class PresupuestoController
    {
        private readonly PresupuestoRepository _repo;
        private readonly CategoriaRepository _catRepo;

        public PresupuestoController()
        {
            _repo    = new PresupuestoRepository();
            _catRepo = new CategoriaRepository();
        }

        public List<PresupuestoResumen> ObtenerResumen(int periodoId) => _repo.ObtenerResumenPorPeriodo(periodoId);

        public List<CategoriaGasto> ObtenerCategoriasSinPresupuesto(int periodoId)
        {
            var asignadas = new HashSet<int>(_repo.ObtenerResumenPorPeriodo(periodoId).Select(r => r.CategoriaId));
            return _catRepo.ObtenerTodas().Where(c => !asignadas.Contains(c.Id)).ToList();
        }

        public Presupuesto Guardar(Presupuesto p)
        {
            if (p.Monto <= 0) throw new ArgumentException("El monto debe ser mayor a cero.");
            if (_repo.ObtenerPorCategoria(p.PeriodoId, p.CategoriaId) != null)
                throw new ArgumentException("Ya existe un presupuesto para esta categoria en este periodo. Editalo en su lugar.");
            return _repo.Guardar(p);
        }

        public void Actualizar(Presupuesto p)
        {
            if (p.Monto <= 0) throw new ArgumentException("El monto debe ser mayor a cero.");
            _repo.Actualizar(p);
        }

        public void Eliminar(int id) => _repo.Eliminar(id);
    }
}
```

- [ ] **Step 2: Register in the `.csproj`**

Find the `<!-- Controllers -->` block:

```xml
    <!-- Controllers -->
    <Compile Include="Controller\DeudaController.cs" />
    <Compile Include="Controller\GastoController.cs" />
    <Compile Include="Controller\IngresoController.cs" />
    <Compile Include="Controller\PeriodoController.cs" />
    <Compile Include="Controller\UsuarioController.cs" />
```

Replace with:

```xml
    <!-- Controllers -->
    <Compile Include="Controller\DeudaController.cs" />
    <Compile Include="Controller\GastoController.cs" />
    <Compile Include="Controller\IngresoController.cs" />
    <Compile Include="Controller\PeriodoController.cs" />
    <Compile Include="Controller\PresupuestoController.cs" />
    <Compile Include="Controller\UsuarioController.cs" />
```

- [ ] **Step 3: Build**

Run the build command from Global Constraints. Expected: success line only.

- [ ] **Step 4: Commit**

```bash
git add GestorIngresosEgresos/Controller/PresupuestoController.cs GestorIngresosEgresos/GestorIngresosEgresos.csproj
git commit -m "Add PresupuestoController with monto/duplicate validation"
```

---

### Task 5: Bloqueo y aviso de presupuesto en `GastoController` + `FormPeriodo`

These two files must change together — `GastoController.Guardar`/`Actualizar` change signature, and `FormPeriodo` is their only caller (verified: `grep -rn "_gastoCtrl\.\(Guardar\|Actualizar\)"` only matches `Vista/FormPeriodo.cs`), so splitting this into two tasks would leave an intermediate non-building state.

**Files:**
- Modify: `GestorIngresosEgresos/Controller/GastoController.cs` (full file)
- Modify: `GestorIngresosEgresos/Vista/FormPeriodo.cs:392-400` (`BtnGasto_Click`) and `:338-367` (`EditarFila`, gasto branch)

**Interfaces:**
- Consumes: `PresupuestoRepository` (Task 3).
- Produces: `GastoController.Guardar(Gasto g, out string avisoPresupuesto)` returns `Gasto`, throws `ArgumentException` if the gasto would exceed its category's available budget. `GastoController.Actualizar(Gasto g, out string avisoPresupuesto)` same validation, excluding the gasto's own prior amount. `avisoPresupuesto` is `null` when there's nothing to show, or a Spanish message when consumption is ≥50%.

- [ ] **Step 1: Replace the full contents of `GastoController.cs`**

```csharp
using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Repository;
using System;
using System.Collections.Generic;

namespace GestorIngresosEgresos.Controller
{
    public class GastoController
    {
        private readonly GastoRepository _repo;
        private readonly CategoriaRepository _catRepo;
        private readonly PresupuestoRepository _presRepo;

        public GastoController()
        {
            _repo     = new GastoRepository();
            _catRepo  = new CategoriaRepository();
            _presRepo = new PresupuestoRepository();
        }

        public List<Gasto> ObtenerPorPeriodo(int periodoId)   => _repo.ObtenerPorPeriodo(periodoId);
        public List<Gasto> ObtenerAbonosPorDeuda(int deudaId) => _repo.ObtenerAbonosPorDeuda(deudaId);
        public List<CategoriaGasto> ObtenerCategorias()       => _catRepo.ObtenerTodas();

        public Gasto Guardar(Gasto g, out string avisoPresupuesto)
        {
            if (g.Monto <= 0)
                throw new ArgumentException("El monto debe ser mayor a cero.");
            if (string.IsNullOrWhiteSpace(g.Descripcion))
                throw new ArgumentException("La descripcion es obligatoria.");
            if (g.Fecha == default) g.Fecha = DateTime.Today;

            ValidarPresupuesto(g, excludeGastoId: null);

            _repo.Guardar(g);
            avisoPresupuesto = CalcularAviso(g);
            return g;
        }

        public void Actualizar(Gasto g, out string avisoPresupuesto)
        {
            if (g.Monto <= 0) throw new ArgumentException("El monto debe ser mayor a cero.");

            ValidarPresupuesto(g, excludeGastoId: g.Id);

            _repo.Actualizar(g);
            avisoPresupuesto = CalcularAviso(g);
        }

        public void Eliminar(int id) => _repo.Eliminar(id);

        private void ValidarPresupuesto(Gasto g, int? excludeGastoId)
        {
            if (!g.CategoriaId.HasValue) return;

            var presupuesto = _presRepo.ObtenerPorCategoria(g.PeriodoId, g.CategoriaId.Value);
            if (presupuesto == null) return;

            decimal gastadoActual = _presRepo.ObtenerGastado(g.PeriodoId, g.CategoriaId.Value, excludeGastoId);
            decimal disponible    = presupuesto.Monto - gastadoActual;
            if (g.Monto > disponible)
                throw new ArgumentException($"Este gasto supera tu presupuesto de {NombreCategoria(g.CategoriaId.Value)}. Disponible: ${disponible:N2}.");
        }

        private string CalcularAviso(Gasto g)
        {
            if (!g.CategoriaId.HasValue) return null;

            var presupuesto = _presRepo.ObtenerPorCategoria(g.PeriodoId, g.CategoriaId.Value);
            if (presupuesto == null || presupuesto.Monto <= 0) return null;

            decimal gastado     = _presRepo.ObtenerGastado(g.PeriodoId, g.CategoriaId.Value, null);
            decimal porcentaje  = Math.Round(gastado / presupuesto.Monto * 100m, 0);
            decimal disponible  = presupuesto.Monto - gastado;
            string  categoria   = NombreCategoria(g.CategoriaId.Value);

            if (porcentaje >= 100)
                return $"Has agotado tu presupuesto de {categoria} este mes.";
            if (porcentaje >= 50)
                return $"Has consumido el {porcentaje:N0}% de tu presupuesto de {categoria}. Te quedan ${disponible:N2}.";
            return null;
        }

        private string NombreCategoria(int categoriaId) =>
            _catRepo.ObtenerTodas().Find(c => c.Id == categoriaId)?.Nombre ?? "esta categoria";
    }
}
```

- [ ] **Step 2: Update `BtnGasto_Click` in `FormPeriodo.cs`**

Find (around line 392):

```csharp
        private void BtnGasto_Click(object sender, EventArgs e)
        {
            using (var form = new FormGasto(_periodo.Id, _gastoCtrl.ObtenerCategorias()))
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try { _gastoCtrl.Guardar(form.Resultado); CargarTabla(); }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
        }
```

Replace with:

```csharp
        private void BtnGasto_Click(object sender, EventArgs e)
        {
            using (var form = new FormGasto(_periodo.Id, _gastoCtrl.ObtenerCategorias()))
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _gastoCtrl.Guardar(form.Resultado, out string aviso);
                        CargarTabla();
                        if (aviso != null)
                            MessageBox.Show(aviso, "Presupuesto", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
        }
```

- [ ] **Step 3: Update the gasto branch of `EditarFila` in `FormPeriodo.cs`**

Find (around line 354, inside `EditarFila`):

```csharp
                else
                {
                    var existing = new Gasto
                    {
                        Id = f.Id, PeriodoId = _periodo.Id, CategoriaId = f.CategoriaId,
                        Fecha = f.Fecha, Descripcion = f.Descripcion, Monto = f.Monto
                    };
                    using (var form = new FormGasto(_periodo.Id, _gastoCtrl.ObtenerCategorias(), existing))
                        if (form.ShowDialog() == DialogResult.OK)
                            { _gastoCtrl.Actualizar(form.Resultado); CargarTabla(); }
                }
```

Replace with:

```csharp
                else
                {
                    var existing = new Gasto
                    {
                        Id = f.Id, PeriodoId = _periodo.Id, CategoriaId = f.CategoriaId,
                        Fecha = f.Fecha, Descripcion = f.Descripcion, Monto = f.Monto
                    };
                    using (var form = new FormGasto(_periodo.Id, _gastoCtrl.ObtenerCategorias(), existing))
                        if (form.ShowDialog() == DialogResult.OK)
                        {
                            _gastoCtrl.Actualizar(form.Resultado, out string aviso);
                            CargarTabla();
                            if (aviso != null)
                                MessageBox.Show(aviso, "Presupuesto", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                }
```

(This stays inside the existing `try { ... } catch (Exception ex) { ... }` that already wraps both branches of `EditarFila` — no change needed to the try/catch itself.)

- [ ] **Step 4: Build**

Run the build command from Global Constraints. Expected: success line only.

- [ ] **Step 5: Manual smoke check (requires the DB migration from Task 1 already applied)**

Run the app (`bin\Debug\GestorIngresosEgresos.exe`), log in, go to "Ingresos y Egresos", register a gasto in a category that has **no** budget yet — it should save normally with no popup (budget features are invisible until a category has a budget assigned, which happens in Task 7). This confirms the change didn't break the existing gasto flow.

- [ ] **Step 6: Commit**

```bash
git add GestorIngresosEgresos/Controller/GastoController.cs GestorIngresosEgresos/Vista/FormPeriodo.cs
git commit -m "Block gastos that exceed their category budget and surface threshold notices"
```

---

### Task 6: `FormPresupuestoDialog` (crear/editar)

**Files:**
- Create: `GestorIngresosEgresos/Vista/FormPresupuestoDialog.cs`
- Modify: `GestorIngresosEgresos/GestorIngresosEgresos.csproj` (register file)

**Interfaces:**
- Consumes: `Presupuesto`, `PresupuestoResumen`, `CategoriaGasto` (Task 2 / existing).
- Produces: `FormPresupuestoDialog(int periodoId, List<CategoriaGasto> categoriasDisponibles)` (modo crear) and `FormPresupuestoDialog(int periodoId, PresupuestoResumen existing)` (modo editar), both exposing `public Presupuesto Resultado { get; }` set only when the dialog closes with `DialogResult.OK` — consumed by `FormPresupuestos` (Task 7).

- [ ] **Step 1: Create `FormPresupuestoDialog.cs`**

```csharp
using GestorIngresosEgresos.Modelo;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GestorIngresosEgresos.Vista
{
    public partial class FormPresupuestoDialog : Form
    {
        static readonly Color C_SURFACE = Color.White;
        static readonly Color C_MUTED   = Color.FromArgb(100, 116, 139);
        static readonly Color C_TEXT    = Color.FromArgb(30, 41, 59);
        static readonly Color C_ACCENT  = Color.FromArgb(37, 99, 235);

        private readonly int  _periodoId;
        private readonly bool _esEdicion;
        private readonly int  _existingId;
        private readonly int  _categoriaId;

        private ComboBox      _cboCategoria;
        private NumericUpDown _nudMonto;

        public Presupuesto Resultado { get; private set; }

        // Modo crear
        public FormPresupuestoDialog(int periodoId, List<CategoriaGasto> categoriasDisponibles)
        {
            _periodoId = periodoId;
            _esEdicion = false;
            InitializeComponent();
            ConstruirUI(categoriasDisponibles, null, 0m);
        }

        // Modo editar
        public FormPresupuestoDialog(int periodoId, PresupuestoResumen existing)
        {
            _periodoId   = periodoId;
            _esEdicion   = true;
            _existingId  = existing.Id;
            _categoriaId = existing.CategoriaId;
            InitializeComponent();
            ConstruirUI(null, existing.CategoriaNombre, existing.Limite);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "FormPresupuestoDialog";
            this.ResumeLayout(false);
        }

        private void ConstruirUI(List<CategoriaGasto> categoriasDisponibles, string categoriaNombreFija, decimal montoActual)
        {
            this.Text            = _esEdicion ? "Editar Presupuesto" : "Nuevo Presupuesto";
            this.ClientSize      = new Size(360, 190);
            this.BackColor       = C_SURFACE;
            this.Font            = new Font("Segoe UI", 10F);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;

            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3,
                Padding = new Padding(24, 20, 24, 16), BackColor = C_SURFACE
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 2; i++) tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Control categoriaControl;
            if (_esEdicion)
            {
                categoriaControl = new Label
                {
                    Text = categoriaNombreFija, Dock = DockStyle.Fill, ForeColor = C_TEXT,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft
                };
            }
            else
            {
                _cboCategoria = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
                foreach (var c in categoriasDisponibles) _cboCategoria.Items.Add(c);
                if (_cboCategoria.Items.Count > 0) _cboCategoria.SelectedIndex = 0;
                categoriaControl = _cboCategoria;
            }

            _nudMonto = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0.01m, Maximum = 9999999m, DecimalPlaces = 2, ThousandsSeparator = true };
            if (_esEdicion) _nudMonto.Value = montoActual;

            tlp.Controls.Add(Lbl("Categoria:"), 0, 0); tlp.Controls.Add(categoriaControl, 1, 0);
            tlp.Controls.Add(Lbl("Monto ($):"), 0, 1); tlp.Controls.Add(_nudMonto,        1, 1);

            var flpBtn = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = C_SURFACE };
            var btnCancelar = new Button { Text = "Cancelar", Size = new Size(90, 32), Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat };
            var btnGuardar  = new Button
            {
                Text = "Guardar", Size = new Size(90, 32), BackColor = C_ACCENT, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            btnGuardar.Click  += BtnGuardar_Click;
            flpBtn.Controls.Add(btnGuardar);
            flpBtn.Controls.Add(btnCancelar);
            tlp.Controls.Add(flpBtn, 0, 2);
            tlp.SetColumnSpan(flpBtn, 2);

            this.Controls.Add(tlp);
            this.AcceptButton = btnGuardar;
            this.CancelButton = btnCancelar;
        }

        private Label Lbl(string t) => new Label
        {
            Text = t, Dock = DockStyle.Fill, ForeColor = C_MUTED,
            Font = new Font("Segoe UI", 10F), TextAlign = ContentAlignment.MiddleRight
        };

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (!_esEdicion && _cboCategoria.SelectedItem == null)
            { MessageBox.Show("Selecciona una categoria.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (_nudMonto.Value <= 0)
            { MessageBox.Show("El monto debe ser mayor a cero.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            int categoriaId = _esEdicion ? _categoriaId : ((CategoriaGasto)_cboCategoria.SelectedItem).Id;

            Resultado = new Presupuesto
            {
                Id          = _existingId,
                PeriodoId   = _periodoId,
                CategoriaId = categoriaId,
                Monto       = _nudMonto.Value
            };
            DialogResult = DialogResult.OK;
        }
    }
}
```

- [ ] **Step 2: Register in the `.csproj`**

Find (in the `<!-- Vistas -->` block, right before `Vista\FormHistorialAbonos.cs`):

```xml
    <Compile Include="Vista\FormGasto.cs">
      <SubType>Form</SubType>
    </Compile>
    <Compile Include="Vista\FormGasto.Designer.cs">
      <DependentUpon>FormGasto.cs</DependentUpon>
    </Compile>
    <Compile Include="Vista\FormHistorialAbonos.cs">
```

Replace with:

```xml
    <Compile Include="Vista\FormGasto.cs">
      <SubType>Form</SubType>
    </Compile>
    <Compile Include="Vista\FormGasto.Designer.cs">
      <DependentUpon>FormGasto.cs</DependentUpon>
    </Compile>
    <Compile Include="Vista\FormPresupuestoDialog.cs">
      <SubType>Form</SubType>
    </Compile>
    <Compile Include="Vista\FormHistorialAbonos.cs">
```

- [ ] **Step 3: Build**

Run the build command from Global Constraints. Expected: success line only.

- [ ] **Step 4: Commit**

```bash
git add GestorIngresosEgresos/Vista/FormPresupuestoDialog.cs GestorIngresosEgresos/GestorIngresosEgresos.csproj
git commit -m "Add FormPresupuestoDialog for creating/editing a category budget"
```

---

### Task 7: `FormPresupuestos` (pantalla principal)

**Files:**
- Create: `GestorIngresosEgresos/Vista/FormPresupuestos.cs`
- Modify: `GestorIngresosEgresos/GestorIngresosEgresos.csproj` (register file)

**Interfaces:**
- Consumes: `PeriodoController.ObtenerOCrearPeriodo(int anio, int mes)` (existing), `PresupuestoController` (Task 4), `PresupuestoResumen`/`EstadoPresupuesto` (Task 2), `FormPresupuestoDialog` (Task 6), `PeriodoManager.Anio/Mes/IrAnterior()/IrSiguiente()/NombrePeriodo` (existing, `Util/PeriodoManager.cs`).
- Produces: `FormPresupuestos` — parameterless constructor, consumed by `FormDashboard` (Task 8).

- [ ] **Step 1: Create `FormPresupuestos.cs`**

```csharp
using GestorIngresosEgresos.Controller;
using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GestorIngresosEgresos.Vista
{
    public partial class FormPresupuestos : Form
    {
        static readonly Color C_SIDEBAR  = Color.FromArgb(30, 41, 59);
        static readonly Color C_BG       = Color.FromArgb(248, 250, 252);
        static readonly Color C_SURFACE  = Color.White;
        static readonly Color C_BORDER   = Color.FromArgb(226, 232, 240);
        static readonly Color C_TEXT     = Color.FromArgb(30, 41, 59);
        static readonly Color C_MUTED    = Color.FromArgb(100, 116, 139);
        static readonly Color C_OK       = Color.FromArgb(16, 185, 129);
        static readonly Color C_ALERTA   = Color.FromArgb(245, 158, 11);
        static readonly Color C_CRITICO  = Color.FromArgb(234, 88, 12);
        static readonly Color C_EXCEDIDO = Color.FromArgb(239, 68, 68);
        static readonly Color C_ACCENT   = Color.FromArgb(37, 99, 235);

        private readonly PeriodoController     _periodoCtrl     = new PeriodoController();
        private readonly PresupuestoController _presupuestoCtrl = new PresupuestoController();

        private Periodo _periodo;
        private Label   _lblNombre, _lblEstado, _lblVacio;
        private Button  _btnAnterior, _btnSiguiente, _btnNuevo;
        private FlowLayoutPanel _panelTarjetas;

        public FormPresupuestos()
        {
            InitializeComponent();
            ConstruirUI();
            CargarPeriodo();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "FormPresupuestos";
            this.ResumeLayout(false);
        }

        private void ConstruirUI()
        {
            this.BackColor = C_BG;
            this.Font      = new Font("Segoe UI", 9.5F);

            var header = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = C_SIDEBAR };

            _btnAnterior  = NavBtn("<");
            _btnSiguiente = NavBtn(">");
            _btnAnterior.Location  = new Point(10, 13);
            _btnSiguiente.Location = new Point(220, 13);

            _lblNombre = new Label
            {
                Location = new Point(46, 15), AutoSize = true,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = Color.White
            };
            _lblEstado = new Label
            {
                Location = new Point(258, 17), Size = new Size(72, 22), AutoSize = false,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter
            };

            _btnAnterior.Click  += (s, e) => { PeriodoManager.IrAnterior();  CargarPeriodo(); };
            _btnSiguiente.Click += (s, e) => { PeriodoManager.IrSiguiente(); CargarPeriodo(); };

            header.Controls.AddRange(new Control[] { _btnAnterior, _lblNombre, _btnSiguiente, _lblEstado });

            var barPanel = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = C_SURFACE, Padding = new Padding(14, 10, 14, 10) };
            _btnNuevo = new Button
            {
                Text = "+ Presupuesto", Size = new Size(130, 30), Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = C_ACCENT, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            _btnNuevo.FlatAppearance.BorderSize = 0;
            _btnNuevo.Click += BtnNuevo_Click;
            var flp = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = C_SURFACE, WrapContents = false };
            flp.Controls.Add(_btnNuevo);
            barPanel.Controls.Add(flp);

            var contentPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14), BackColor = C_BG };
            _panelTarjetas = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = C_BG };
            _lblVacio = new Label
            {
                Text = "No hay presupuestos asignados este mes. Usa \"+ Presupuesto\" para separar un monto por categoria.",
                AutoSize = true, ForeColor = C_MUTED, Font = new Font("Segoe UI", 10F), Location = new Point(4, 4), Visible = false
            };
            contentPanel.Controls.Add(_lblVacio);
            contentPanel.Controls.Add(_panelTarjetas);

            this.Controls.Add(contentPanel);
            this.Controls.Add(barPanel);
            this.Controls.Add(header);
        }

        private Button NavBtn(string t) => new Button
        {
            Text = t, Size = new Size(30, 30), Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 163, 184), BackColor = C_SIDEBAR, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };

        private void CargarPeriodo()
        {
            _periodo = _periodoCtrl.ObtenerOCrearPeriodo(PeriodoManager.Anio, PeriodoManager.Mes);

            _lblNombre.Text = _periodo?.Nombre ?? Capitalizar(PeriodoManager.NombrePeriodo);
            bool sinDatos = _periodo == null;
            bool abierto  = _periodo?.Estado == EstadoPeriodo.ABIERTO;

            _lblEstado.Text      = sinDatos ? "SIN DATOS" : (abierto ? "ABIERTO" : "CERRADO");
            _lblEstado.BackColor = sinDatos ? Color.FromArgb(51, 65, 85) : (abierto ? Color.FromArgb(6, 78, 59) : Color.FromArgb(69, 26, 3));
            _lblEstado.ForeColor = sinDatos ? C_MUTED : (abierto ? C_OK : Color.FromArgb(245, 158, 11));
            _btnNuevo.Enabled    = abierto;

            if (sinDatos) { MostrarTarjetas(new List<PresupuestoResumen>()); return; }
            CargarTarjetas();
        }

        private void CargarTarjetas() => MostrarTarjetas(_presupuestoCtrl.ObtenerResumen(_periodo.Id));

        private void MostrarTarjetas(List<PresupuestoResumen> resumen)
        {
            _panelTarjetas.Controls.Clear();
            _lblVacio.Visible = resumen.Count == 0;
            foreach (var r in resumen)
                _panelTarjetas.Controls.Add(CrearTarjeta(r));
        }

        private Panel CrearTarjeta(PresupuestoResumen r)
        {
            Color colorEstado =
                r.Estado == EstadoPresupuesto.EXCEDIDO ? C_EXCEDIDO :
                r.Estado == EstadoPresupuesto.CRITICO  ? C_CRITICO  :
                r.Estado == EstadoPresupuesto.ALERTA   ? C_ALERTA   : C_OK;

            var card = new Panel { Size = new Size(260, 150), Margin = new Padding(0, 0, 14, 14), BackColor = C_SURFACE };
            card.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle,
                C_BORDER, 1, ButtonBorderStyle.Solid, C_BORDER, 1, ButtonBorderStyle.Solid,
                C_BORDER, 1, ButtonBorderStyle.Solid, C_BORDER, 1, ButtonBorderStyle.Solid);

            var lblNombre = new Label
            {
                Text = r.CategoriaNombre, Location = new Point(14, 12), AutoSize = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = C_TEXT
            };

            var barraFondo = new Panel { Location = new Point(14, 42), Size = new Size(232, 10), BackColor = C_BORDER };
            decimal fraccion = Math.Min(r.Porcentaje / 100m, 1m);
            var barraRelleno = new Panel
            {
                Location  = new Point(0, 0),
                Size      = new Size((int)(232 * fraccion), 10),
                BackColor = colorEstado
            };
            barraFondo.Controls.Add(barraRelleno);

            var lblMonto = new Label
            {
                Text = $"${r.Gastado:N2} / ${r.Limite:N2}", Location = new Point(14, 60), AutoSize = true,
                Font = new Font("Segoe UI", 9.5F), ForeColor = C_MUTED
            };
            var lblPorcentaje = new Label
            {
                Text = $"{r.Porcentaje:N0}%", Location = new Point(14, 82), AutoSize = true,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = colorEstado
            };
            var lblDisponible = new Label
            {
                Text = r.Disponible >= 0 ? $"Disponible: ${r.Disponible:N2}" : $"Excedido por: ${-r.Disponible:N2}",
                Location = new Point(14, 112), AutoSize = true,
                Font = new Font("Segoe UI", 9F), ForeColor = C_MUTED
            };

            var btnEditar = new Button
            {
                Text = "Editar", Size = new Size(60, 24), Location = new Point(122, 112),
                Font = new Font("Segoe UI", 8F), ForeColor = C_MUTED, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnEditar.FlatAppearance.BorderSize = 0;
            btnEditar.Click += (s, e) => EditarPresupuesto(r);

            var btnEliminar = new Button
            {
                Text = "Eliminar", Size = new Size(70, 24), Location = new Point(182, 112),
                Font = new Font("Segoe UI", 8F), ForeColor = C_EXCEDIDO, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnEliminar.FlatAppearance.BorderSize = 0;
            btnEliminar.Click += (s, e) => EliminarPresupuesto(r);

            if (_periodo.Estado != EstadoPeriodo.ABIERTO) { btnEditar.Enabled = false; btnEliminar.Enabled = false; }

            card.Controls.AddRange(new Control[] { lblNombre, barraFondo, lblMonto, lblPorcentaje, lblDisponible, btnEditar, btnEliminar });
            return card;
        }

        private void BtnNuevo_Click(object sender, EventArgs e)
        {
            var disponibles = _presupuestoCtrl.ObtenerCategoriasSinPresupuesto(_periodo.Id);
            if (disponibles.Count == 0)
            {
                MessageBox.Show("Ya asignaste un presupuesto a todas las categorias este mes.", "Presupuestos",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var form = new FormPresupuestoDialog(_periodo.Id, disponibles))
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try { _presupuestoCtrl.Guardar(form.Resultado); CargarTarjetas(); }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
        }

        private void EditarPresupuesto(PresupuestoResumen r)
        {
            using (var form = new FormPresupuestoDialog(_periodo.Id, r))
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try { _presupuestoCtrl.Actualizar(form.Resultado); CargarTarjetas(); }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
        }

        private void EliminarPresupuesto(PresupuestoResumen r)
        {
            if (MessageBox.Show($"Eliminar el presupuesto de \"{r.CategoriaNombre}\"?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try { _presupuestoCtrl.Eliminar(r.Id); CargarTarjetas(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private static string Capitalizar(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
    }
}
```

- [ ] **Step 2: Register in the `.csproj`**

Find (in the `<!-- Vistas -->` block, right before `Vista\FormPeriodo.cs`):

```xml
    <Compile Include="Vista\FormNuevaDeuda.cs">
      <SubType>Form</SubType>
    </Compile>
    <Compile Include="Vista\FormNuevaDeuda.Designer.cs">
      <DependentUpon>FormNuevaDeuda.cs</DependentUpon>
    </Compile>
    <Compile Include="Vista\FormPeriodo.cs">
```

Replace with:

```xml
    <Compile Include="Vista\FormNuevaDeuda.cs">
      <SubType>Form</SubType>
    </Compile>
    <Compile Include="Vista\FormNuevaDeuda.Designer.cs">
      <DependentUpon>FormNuevaDeuda.cs</DependentUpon>
    </Compile>
    <Compile Include="Vista\FormPresupuestos.cs">
      <SubType>Form</SubType>
    </Compile>
    <Compile Include="Vista\FormPeriodo.cs">
```

- [ ] **Step 3: Build**

Run the build command from Global Constraints. Expected: success line only.

- [ ] **Step 4: Commit**

```bash
git add GestorIngresosEgresos/Vista/FormPresupuestos.cs GestorIngresosEgresos/GestorIngresosEgresos.csproj
git commit -m "Add FormPresupuestos screen with per-category progress cards"
```

---

### Task 8: Sidebar — botón "Presupuestos" en `FormDashboard`

**Files:**
- Modify: `GestorIngresosEgresos/Vista/FormDashboard.cs:52-60` (nav buttons + panel wiring)

**Interfaces:**
- Consumes: `FormPresupuestos` (Task 7).

- [ ] **Step 1: Add the nav button**

Find (around line 52):

```csharp
            var btnIngresos = CrearNavBtn("Ingresos y Egresos");
            var btnDeudas   = CrearNavBtn("Deudas");

            btnIngresos.Click += (s, e) => { AbrirForm(new FormPeriodo()); MarcarActivo(btnIngresos); };
            btnDeudas.Click   += (s, e) => { AbrirForm(new FormDeudas()); MarcarActivo(btnDeudas); };

            var panelNav = new Panel { Dock = DockStyle.Fill, BackColor = C_SIDEBAR };
            panelNav.Controls.Add(btnDeudas);
            panelNav.Controls.Add(btnIngresos);
```

Replace with:

```csharp
            var btnIngresos     = CrearNavBtn("Ingresos y Egresos");
            var btnPresupuestos = CrearNavBtn("Presupuestos");
            var btnDeudas       = CrearNavBtn("Deudas");

            btnIngresos.Click     += (s, e) => { AbrirForm(new FormPeriodo()); MarcarActivo(btnIngresos); };
            btnPresupuestos.Click += (s, e) => { AbrirForm(new FormPresupuestos()); MarcarActivo(btnPresupuestos); };
            btnDeudas.Click       += (s, e) => { AbrirForm(new FormDeudas()); MarcarActivo(btnDeudas); };

            var panelNav = new Panel { Dock = DockStyle.Fill, BackColor = C_SIDEBAR };
            panelNav.Controls.Add(btnDeudas);
            panelNav.Controls.Add(btnPresupuestos);
            panelNav.Controls.Add(btnIngresos);
```

(Controls are added in reverse visual order because they're `Dock = DockStyle.Top` — the last one added ends up on top. This keeps "Ingresos y Egresos" first, "Presupuestos" second, "Deudas" third, matching the existing `FormPeriodo` reverse-order comment/pattern.)

- [ ] **Step 2: Build**

Run the build command from Global Constraints. Expected: success line only.

- [ ] **Step 3: Commit**

```bash
git add GestorIngresosEgresos/Vista/FormDashboard.cs
git commit -m "Add Presupuestos entry to the sidebar navigation"
```

---

### Task 9: QA manual end-to-end

No new files. This is the manual verification called for in the spec's Testing section — there's no automated GUI test harness in this project, and the spec explicitly opted out of adding one.

**Files:** none

**Interfaces:** none (uses everything built in Tasks 1–8)

- [ ] **Step 1: Apply the migration**

Apply `docs/sql/migration_v3.sql` to the local `GestorIngresosDB` database using whatever MySQL client was used for the existing `migration.sql`/`migration_v2.sql` (e.g. MySQL Workbench, HeidiSQL, or the `mysql` CLI).

- [ ] **Step 2: Run the app and log in**

Launch `GestorIngresosEgresos/bin/Debug/GestorIngresosEgresos.exe`, log in.

- [ ] **Step 3: Create a budget**

Go to "Presupuestos" in the sidebar. Click "+ Presupuesto", assign e.g. $20 to "Transporte", save. Confirm a card appears showing "$0.00 / $20.00", 0%, "Disponible: $20.00", green bar.

- [ ] **Step 4: Cross 50%**

Go to "Ingresos y Egresos", register a gasto of $10 in "Transporte". Confirm it saves and a "Presupuesto" info popup appears saying something like "Has consumido el 50% de tu presupuesto de Transporte. Te quedan $10.00." Go back to "Presupuestos" and confirm the card now shows 50%, amber bar.

- [ ] **Step 5: Cross 80% and then attempt to exceed**

Register a gasto of $3.50 in "Transporte" (now at 67.5% — no popup expected, still under 80%; if the popup doesn't appear, that's correct). Register another $3 gasto (now at 82.5% — should trigger the ≥80% popup). Confirm the card shows the orange/critical color. Then attempt a gasto of $5 in "Transporte" (would push it past the ~$3.50 remaining) — confirm it is **blocked** with an error message mentioning the available amount, and that the gasto was **not** saved (check the movement list and the card didn't change).

- [ ] **Step 6: Full exhaustion**

Register a gasto that exactly uses up the remaining budget. Confirm the card shows 100%, red bar, "Disponible: $0.00", and that saving one more gasto of any positive amount in that category is blocked.

- [ ] **Step 7: Period isolation**

Navigate to a different month (use "<" to go to a previous period). Confirm "Presupuestos" shows no cards for that period (budgets don't leak across periods), and that a gasto in "Transporte" in that other period is not blocked by the current period's budget.

- [ ] **Step 8: Closed period**

If there's a way to close a period in this app (check `PeriodoController.CerrarPeriodo` / any "Cerrar periodo" UI action), close a period and confirm "+ Presupuesto" and the Editar/Eliminar buttons on its cards are disabled.

- [ ] **Step 9: Report results**

No commit for this task — if any step fails, go back to the relevant earlier task, fix, rebuild, and re-run the affected steps of this checklist.

---

## Self-Review

**Spec coverage:**
- Modelo de datos (tabla + `Presupuesto`/`PresupuestoResumen`) → Tasks 1–2. ✓
- `PresupuestoRepository` (resumen, gastado, CRUD) → Task 3. ✓
- `PresupuestoController` (validaciones, categorías sin presupuesto) → Task 4. ✓
- Bloqueo de sobregiro + aviso de umbral en `GastoController`/`FormPeriodo` → Task 5. ✓
- `FormPresupuestoDialog` (crear/editar) → Task 6. ✓
- `FormPresupuestos` (tarjetas, colores por estado, header de periodo) → Task 7. ✓
- Botón de sidebar en `FormDashboard` → Task 8. ✓
- Validaciones (monto > 0, sin duplicados, deshabilitado si `CERRADO`) → Tasks 4, 6, 7. ✓
- Notificaciones de umbral (50/80/100%) → Task 5. ✓
- Verificación manual per spec's Testing section → Task 9. ✓
- Self-check for the one piece of non-trivial pure logic (ponytail requirement beyond spec) → Task 2. ✓

**Placeholder scan:** no "TBD"/"TODO"/"implement later" found; every step has complete, runnable code.

**Type consistency:** `PresupuestoResumen` fields (`Id`, `CategoriaId`, `CategoriaNombre`, `Limite`, `Gastado`, `Disponible`, `Porcentaje`, `Estado`) are the same across Task 2 (definition), Task 3 (repository mapping), Task 7 (`FormPresupuestos`/`FormPresupuestoDialog` usage). `Presupuesto` fields (`Id`, `PeriodoId`, `CategoriaId`, `Monto`) match across Tasks 2, 3, 4, 6. `GastoController.Guardar`/`Actualizar` signature `(Gasto g, out string avisoPresupuesto)` matches between Task 5's controller definition and its `FormPeriodo` call sites (Task 5, same task). `PresupuestoRepository.ObtenerGastado(int, int, int?)` signature matches between Task 3's definition and Task 5's two call sites (`excludeGastoId: null` / `excludeGastoId: g.Id`).

**Scope check:** single cohesive feature (budgets), no unrelated subsystems bundled in. Right-sized for one plan.
