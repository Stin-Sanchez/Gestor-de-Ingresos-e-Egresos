# Presupuestos por categoría — Diseño

Fecha: 2026-08-20

## Objetivo

Permitir separar un monto por categoría de gasto dentro del periodo (mes) actual (ej. $20 para Transporte), consumirlo a medida que se registran gastos de esa categoría, y mostrar un estado dinámico (barra de progreso + % consumido + monto disponible) con validación de sobregiro y aviso al cruzar umbrales de consumo.

## Alcance

- El presupuesto se define **por categoría y por periodo** (mensual). Al abrir un nuevo periodo no hay presupuestos asignados; se vuelven a definir cada mes.
- Si un gasto excede el disponible de su categoría, se **bloquea** el guardado (no se permite sobregirar).
- Al guardar un gasto que cruza 50% / 80% / 100% de consumo de su categoría, se muestra un aviso informativo con el porcentaje consumido y el monto restante.
- Nueva pantalla dedicada "Presupuestos" en el sidebar, con tarjetas por categoría (estilo consistente con el resto de la app).
- Fuera de alcance: presupuestos que no sean por categoría/periodo, notificaciones fuera de la app (email, push del sistema), presupuesto agregado por período completo (solo por categoría).

## Modelo de datos

Nueva tabla `presupuestos` (migración `docs/sql/migration_v3.sql`):

```sql
CREATE TABLE presupuestos (
  id INT AUTO_INCREMENT PRIMARY KEY,
  periodo_id INT NOT NULL,
  categoria_id INT NOT NULL,
  monto DECIMAL(12,2) NOT NULL,
  UNIQUE KEY uq_periodo_categoria (periodo_id, categoria_id),
  FOREIGN KEY (periodo_id) REFERENCES periodos(id),
  FOREIGN KEY (categoria_id) REFERENCES categorias_gasto(id)
);
```

El monto "gastado" **no se almacena**: se calcula siempre en tiempo real sumando `gastos.monto` donde `periodo_id` y `categoria_id` coinciden y `deuda_id IS NULL` (excluye abonos de deuda, que no son gasto de categoría).

`Modelo/Presupuesto.cs`:
```csharp
public class Presupuesto
{
    public int Id { get; set; }
    public int PeriodoId { get; set; }
    public int CategoriaId { get; set; }
    public decimal Monto { get; set; }
}
```

`Modelo/PresupuestoResumen.cs` (vista calculada, no tabla):
```csharp
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
}

public enum EstadoPresupuesto { OK, ALERTA, CRITICO, EXCEDIDO }
```

## Backend

**`Repository/PresupuestoRepository.cs`**
- `ObtenerResumenPorPeriodo(periodoId)` → `List<PresupuestoResumen>`, un SQL con `JOIN`/`LEFT JOIN` + `GROUP BY` sobre `presupuestos`, `categorias_gasto`, `gastos`.
- `ObtenerPorCategoria(periodoId, categoriaId)` → `Presupuesto` o `null` (usado por validación de gasto).
- `ObtenerGastado(periodoId, categoriaId, excludeGastoId = null)` → `decimal` (usado por validación de gasto; excluye el propio gasto cuando se está editando).
- `Guardar(Presupuesto p)`, `Actualizar(Presupuesto p)`, `Eliminar(int id)`.

**`Controller/PresupuestoController.cs`**
- `ObtenerResumen(periodoId)` → delega al repo.
- `ObtenerCategoriasSinPresupuesto(periodoId)` → categorías totales menos las que ya tienen presupuesto asignado ese periodo (para el combo del diálogo "+ Presupuesto").
- `Guardar(Presupuesto p)`: valida `Monto > 0`; valida que no exista ya presupuesto para esa categoría/periodo (mensaje claro si ya existe, sugiriendo editar).
- `Actualizar(Presupuesto p)`: valida `Monto > 0`.
- `Eliminar(int id)`.

**`Controller/GastoController.cs`** (modificado)
- Nueva sobrecarga `Gasto Guardar(Gasto g, out string avisoPresupuesto)`:
  1. Validaciones existentes (monto > 0, descripción obligatoria).
  2. Si `g.CategoriaId` tiene un presupuesto asociado en el periodo: `disponible = limite - gastado_actual`. Si `g.Monto > disponible` → `throw new ArgumentException($"Este gasto supera tu presupuesto de {categoria}. Disponible: ${disponible:N2}")` (bloquea, no se guarda).
  3. Guarda el gasto.
  4. Si tiene presupuesto, recalcula el % consumido tras el guardado; si ≥50% arma `avisoPresupuesto` con el mensaje de umbral correspondiente (50/80/100+); si no, `avisoPresupuesto = null`.
- Misma lógica (validación de disponible excluyendo el propio id, y aviso) se aplica en una sobrecarga `Actualizar(Gasto g, out string avisoPresupuesto)`.
- El `Guardar`/`Actualizar` sin `out` existentes se mantienen como wrappers para no romper otros llamadores, si los hubiera (revisar en implementación; si `FormPeriodo` es el único caller, se migra directamente y se eliminan las firmas viejas).

## UI

**Nueva pantalla `Vista/FormPresupuestos.cs`** (mismo estilo que `FormPeriodo`: paleta de colores, header oscuro con nombre/nav de periodo):
- Header: nombre del periodo actual + navegación `<`/`>` (comparte `PeriodoManager`, igual que `FormPeriodo`).
- Grid de tarjetas (una por categoría con presupuesto asignado ese periodo): nombre de categoría, barra de progreso coloreada por `EstadoPresupuesto` (verde/ámbar/naranja/rojo), texto `"$gastado / $límite"`, texto `"Disponible: $x"`, botones editar/eliminar.
- Botón "+ Presupuesto" (deshabilitado si el periodo está `CERRADO`, igual criterio que `+Gasto`/`+Ingreso` en `FormPeriodo`) que abre un diálogo pequeño (estilo `FormGasto`): combo de categorías sin presupuesto aún + monto.
- Editar una tarjeta abre el mismo diálogo pre-cargado (solo el monto es editable, categoría fija).
- Eliminar pide confirmación (`MessageBox` sí/no, mismo patrón que `EliminarFila` en `FormPeriodo`).

**`Vista/FormDashboard.cs`** (modificado): nuevo botón de navegación "Presupuestos" en el sidebar entre "Ingresos y Egresos" y "Deudas", que abre `FormPresupuestos`.

**`Vista/FormPeriodo.cs`** (modificado): en `BtnGasto_Click` y `EditarFila` (rama gasto), usar la nueva sobrecarga `Guardar/Actualizar(gasto, out aviso)`; si `aviso != null`, mostrar `MessageBox.Show(aviso, "Presupuesto", MessageBoxButtons.OK, MessageBoxIcon.Information)` después de refrescar la tabla. Las excepciones de bloqueo por sobregiro se muestran igual que hoy (ya hay un `try/catch` que muestra `ex.Message`).

## Validaciones

- Monto de presupuesto > 0.
- No duplicar presupuesto para la misma categoría dentro del mismo periodo (constraint `UNIQUE` + validación en controller con mensaje claro).
- Un gasto con categoría presupuestada no puede exceder el disponible de esa categoría (bloqueo duro, ver arriba).
- Todas las acciones de presupuesto (crear/editar/eliminar) deshabilitadas si el periodo está `CERRADO`, igual que Ingreso/Gasto.

## Notificaciones (umbrales)

Al guardar/editar un gasto en una categoría presupuestada, si el % de consumo resultante:
- ≥100%: `"Has agotado tu presupuesto de {categoria} este mes."`
- ≥80% (y <100%): `"Has consumido el {pct}% de tu presupuesto de {categoria}. Te quedan ${disponible:N2}."`
- ≥50% (y <80%): mismo formato que el anterior.
- <50%: sin aviso.

No se rastrea "cruce" de umbral entre guardados (no hay estado previo persistido); simplemente se evalúa el % resultante tras cada guardado. Esto es intencionalmente simple: si el usuario guarda dos gastos seguidos que lo mantienen en la misma banda (ej. 55% → 60%), verá el aviso ambas veces. Aceptable para el alcance actual.

## Testing / verificación

- Verificación manual en la app (WinForms): crear presupuesto, registrar gastos que crucen 50/80/100%, verificar bloqueo al exceder, verificar que la tarjeta y el color cambian dinámicamente, verificar que presupuestos no se filtran entre periodos distintos.
- No hay suite de tests automatizados existente en el proyecto; no se introduce framework de testing solo para esta feature (fuera de alcance).
