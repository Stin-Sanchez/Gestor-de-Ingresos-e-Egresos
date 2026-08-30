using GestorIngresosEgresos.Api.Modelo;
using GestorIngresosEgresos.Api.Repository;

namespace GestorIngresosEgresos.Api.Services;

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
            throw new ArgumentException("El acreedor es obligatorio.");
        if (d.MontoOriginal <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero.");
        if (d.FechaInicio == default) d.FechaInicio = DateTime.Today;
        d.UsuarioId = usuarioId;
        return repo.Guardar(d);
    }

    public void Eliminar(int usuarioId, int id) => repo.Eliminar(usuarioId, id);

    // Crea Gasto en el periodo indicado + actualiza la deuda en una transaccion.
    public Gasto RegistrarAbono(int usuarioId, int deudaId, int periodoId, int? categoriaId, decimal monto, string? descripcion)
    {
        if (monto <= 0) throw new ArgumentException("El monto debe ser mayor a cero.");

        var d = repo.ObtenerPorId(usuarioId, deudaId)
            ?? throw new KeyNotFoundException("Deuda no encontrada.");
        if (periodoRepo.ObtenerPorId(usuarioId, periodoId) is null)
            throw new KeyNotFoundException("Periodo no encontrado.");

        if (d.Estado == EstadoDeuda.PAGADA)
            throw new InvalidOperationException("La deuda ya esta completamente pagada.");

        if (monto > d.SaldoPendiente)
            throw new ArgumentException($"El abono no puede superar el saldo pendiente (${d.SaldoPendiente:N2}).");

        return repo.RegistrarAbono(deudaId, periodoId, categoriaId, monto, descripcion);
    }

    public decimal TotalPendiente(int usuarioId) =>
        repo.ObtenerTodas(usuarioId).Where(d => d.Estado == EstadoDeuda.ACTIVA).Sum(d => d.SaldoPendiente);
}
