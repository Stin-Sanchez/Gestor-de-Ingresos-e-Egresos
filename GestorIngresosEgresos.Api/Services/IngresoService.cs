using GestorIngresosEgresos.Api.Modelo;
using GestorIngresosEgresos.Api.Repository;

namespace GestorIngresosEgresos.Api.Services;

public class IngresoService(IngresoRepository repo, PeriodoRepository periodoRepo)
{
    public List<Ingreso> ObtenerPorPeriodo(int usuarioId, int periodoId) => repo.ObtenerPorPeriodo(usuarioId, periodoId);

    public Ingreso Guardar(int usuarioId, Ingreso ing)
    {
        if (periodoRepo.ObtenerPorId(usuarioId, ing.PeriodoId) is null)
            throw new KeyNotFoundException("Periodo no encontrado.");
        if (ing.Monto <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero.");
        if (string.IsNullOrWhiteSpace(ing.Descripcion))
            throw new ArgumentException("La descripcion es obligatoria.");
        if (ing.Fecha == default) ing.Fecha = DateTime.Today;
        return repo.Guardar(ing);
    }

    public void Actualizar(int usuarioId, Ingreso ing)
    {
        if (repo.ObtenerPorId(usuarioId, ing.Id) is null)
            throw new KeyNotFoundException("Ingreso no encontrado.");
        if (ing.Monto <= 0) throw new ArgumentException("El monto debe ser mayor a cero.");
        repo.Actualizar(usuarioId, ing);
    }

    public void Eliminar(int usuarioId, int id)
    {
        if (repo.ObtenerPorId(usuarioId, id) is null)
            throw new KeyNotFoundException("Ingreso no encontrado.");
        repo.Eliminar(usuarioId, id);
    }
}
