using GestorIngresosEgresos.Api.Modelo;
using GestorIngresosEgresos.Api.Repository;

namespace GestorIngresosEgresos.Api.Services;

// Totales separados por direccion: mezclar lo que debo con lo que me deben
// daria un numero que no significa nada.
public record ResumenDeudas(decimal Debo, decimal MeDeben, int ActivasDebo, int ActivasMeDeben)
{
    public decimal Neto => MeDeben - Debo;
}

public class DeudaService(DeudaRepository repo, PeriodoRepository periodoRepo)
{
    public List<Deuda> ObtenerTodas(int usuarioId) => repo.ObtenerTodas(usuarioId);

    public List<Deuda> ObtenerActivas(int usuarioId) =>
        repo.ObtenerTodas(usuarioId).Where(d => d.Estado == EstadoDeuda.ACTIVA).ToList();

    public Deuda Guardar(int usuarioId, Deuda d)
    {
        if (string.IsNullOrWhiteSpace(d.Nombre))
            throw new ArgumentException("El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(d.Acreedor))
            throw new ArgumentException(d.Tipo == TipoDeuda.DEBO
                ? "El acreedor es obligatorio."
                : "Indica quien te debe.");
        if (d.MontoOriginal <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero.");
        if (d.FechaInicio == default) d.FechaInicio = DateTime.Today;
        d.UsuarioId = usuarioId;
        return repo.Guardar(d);
    }

    public void Eliminar(int usuarioId, int id) => repo.Eliminar(usuarioId, id);

    public List<Deuda.Movimiento> ObtenerPagos(int usuarioId, int deudaId)
    {
        var d = repo.ObtenerPorId(usuarioId, deudaId)
            ?? throw new KeyNotFoundException("Deuda no encontrada.");
        return repo.ObtenerPagos(usuarioId, d.Tipo, deudaId);
    }

    // Un abono (deuda que debo) se registra como gasto y baja el saldo del periodo;
    // un cobro (deuda que me deben) se registra como ingreso y lo sube.
    public Deuda.Movimiento RegistrarPago(int usuarioId, int deudaId, int periodoId, int? categoriaId, decimal monto, string? descripcion)
    {
        if (monto <= 0) throw new ArgumentException("El monto debe ser mayor a cero.");

        var d = repo.ObtenerPorId(usuarioId, deudaId)
            ?? throw new KeyNotFoundException("Deuda no encontrada.");
        if (periodoRepo.ObtenerPorId(usuarioId, periodoId) is null)
            throw new KeyNotFoundException("Periodo no encontrado.");

        if (d.Estado == EstadoDeuda.PAGADA)
            throw new InvalidOperationException(d.Tipo == TipoDeuda.DEBO
                ? "La deuda ya esta completamente pagada."
                : "Ya te pagaron toda esta deuda.");

        if (monto > d.SaldoPendiente)
            throw new ArgumentException($"El monto no puede superar el saldo pendiente (${d.SaldoPendiente:N2}).");

        return repo.RegistrarPago(d.Tipo, deudaId, periodoId, categoriaId, monto, descripcion);
    }

    public ResumenDeudas Resumen(int usuarioId)
    {
        var activas = repo.ObtenerTodas(usuarioId).Where(d => d.Estado == EstadoDeuda.ACTIVA).ToList();
        var debo = activas.Where(d => d.Tipo == TipoDeuda.DEBO).ToList();
        var meDeben = activas.Where(d => d.Tipo == TipoDeuda.ME_DEBEN).ToList();

        return new ResumenDeudas(
            Debo: debo.Sum(d => d.SaldoPendiente),
            MeDeben: meDeben.Sum(d => d.SaldoPendiente),
            ActivasDebo: debo.Count,
            ActivasMeDeben: meDeben.Count);
    }
}
