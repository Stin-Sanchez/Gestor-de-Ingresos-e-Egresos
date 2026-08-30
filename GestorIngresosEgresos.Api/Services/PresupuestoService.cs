using GestorIngresosEgresos.Api.Modelo;
using GestorIngresosEgresos.Api.Repository;

namespace GestorIngresosEgresos.Api.Services;

public record ConsumoGuardado(ConsumoDto Consumo, string? Aviso);

// Coordina los sobres y sus consumos. Un consumo no puede pasarse del sobre: se bloquea,
// igual que se decidio para los gastos contra su presupuesto.
public class PresupuestoService(PresupuestoRepository repo)
{
    public List<PresupuestoResumen> ObtenerSobres(int usuarioId, int periodoId) => repo.ObtenerSobresPorPeriodo(usuarioId, periodoId);
    public PresupuestoResumen? ObtenerResumen(int usuarioId, int gastoId) => repo.ObtenerResumenPorGasto(usuarioId, gastoId);
    public List<Consumo> ObtenerConsumos(int usuarioId, int gastoId) => repo.ObtenerConsumos(usuarioId, gastoId);

    public ConsumoGuardado Guardar(int usuarioId, Consumo c)
    {
        Validar(usuarioId, c, excludeConsumoId: null);
        repo.Guardar(c);
        return new ConsumoGuardado(ConsumoDto.De(c), CalcularAviso(usuarioId, c.GastoId));
    }

    public ConsumoGuardado Actualizar(int usuarioId, Consumo c)
    {
        c.GastoId = repo.ObtenerGastoIdDeConsumo(usuarioId, c.Id)
            ?? throw new KeyNotFoundException("Consumo no encontrado.");
        Validar(usuarioId, c, excludeConsumoId: c.Id);
        repo.Actualizar(usuarioId, c);
        return new ConsumoGuardado(ConsumoDto.De(c), CalcularAviso(usuarioId, c.GastoId));
    }

    public void Eliminar(int usuarioId, int consumoId) => repo.Eliminar(usuarioId, consumoId);

    private void Validar(int usuarioId, Consumo c, int? excludeConsumoId)
    {
        if (c.Monto <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero.");
        if (string.IsNullOrWhiteSpace(c.Descripcion))
            throw new ArgumentException("La descripcion es obligatoria.");
        if (c.Fecha == default) c.Fecha = DateTime.Today;

        var sobre = repo.ObtenerResumenPorGasto(usuarioId, c.GastoId)
            ?? throw new ArgumentException("El sobre ya no existe.");

        decimal consumido = repo.ObtenerConsumido(c.GastoId, excludeConsumoId);
        if (!PresupuestoResumen.Excede(c.Monto, sobre.Limite, consumido)) return;

        decimal disponible = sobre.Limite - consumido;
        throw new ArgumentException(disponible <= 0
            ? $"Ya consumiste todo el sobre \"{sobre.Titulo}\". Ajusta el monto del egreso si necesitas mas."
            : $"Este consumo supera lo que queda en \"{sobre.Titulo}\". Disponible: ${disponible:N2}.");
    }

    private string? CalcularAviso(int usuarioId, int gastoId)
    {
        var sobre = repo.ObtenerResumenPorGasto(usuarioId, gastoId);
        if (sobre is null || sobre.Limite <= 0) return null;

        if (sobre.PorcentajeMostrado >= 100)
            return $"Consumiste todo el sobre \"{sobre.Titulo}\".";
        if (sobre.PorcentajeMostrado >= 50)
            return $"Llevas el {sobre.PorcentajeMostrado:N0}% del sobre \"{sobre.Titulo}\". Te quedan ${sobre.Disponible:N2}.";
        return null;
    }
}
