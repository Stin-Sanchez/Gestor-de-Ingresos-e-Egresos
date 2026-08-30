using GestorIngresosEgresos.Api.Modelo;
using GestorIngresosEgresos.Api.Repository;

namespace GestorIngresosEgresos.Api.Services;

public class GastoService(GastoRepository repo, PeriodoRepository periodoRepo, PresupuestoRepository presRepo, CategoriaRepository catRepo)
{
    public List<Gasto> ObtenerPorPeriodo(int usuarioId, int periodoId) => repo.ObtenerPorPeriodo(usuarioId, periodoId);
    public List<Gasto> ObtenerAbonosPorDeuda(int usuarioId, int deudaId) => repo.ObtenerAbonosPorDeuda(usuarioId, deudaId);
    public List<CategoriaGasto> ObtenerCategorias() => catRepo.ObtenerTodas();

    public Gasto Guardar(int usuarioId, Gasto g)
    {
        if (periodoRepo.ObtenerPorId(usuarioId, g.PeriodoId) is null)
            throw new KeyNotFoundException("Periodo no encontrado.");
        if (g.Monto <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero.");
        if (string.IsNullOrWhiteSpace(g.Descripcion))
            throw new ArgumentException("La descripcion es obligatoria.");
        if (g.Fecha == default) g.Fecha = DateTime.Today;
        return repo.Guardar(g);
    }

    public void Actualizar(int usuarioId, Gasto g)
    {
        if (repo.ObtenerPorId(usuarioId, g.Id) is null)
            throw new KeyNotFoundException("Gasto no encontrado.");
        if (g.Monto <= 0) throw new ArgumentException("El monto debe ser mayor a cero.");
        if (string.IsNullOrWhiteSpace(g.Descripcion))
            throw new ArgumentException("La descripcion es obligatoria.");

        ValidarContraConsumos(g);
        repo.Actualizar(usuarioId, g);
    }

    public void Eliminar(int usuarioId, int id)
    {
        if (repo.ObtenerPorId(usuarioId, id) is null)
            throw new KeyNotFoundException("Gasto no encontrado.");
        repo.Eliminar(usuarioId, id);
    }

    // Un sobre no puede quedar por debajo de lo que ya se consumio de el, ni dejar de
    // ser sobre mientras tenga consumos: en ambos casos el registro quedaria incoherente.
    private void ValidarContraConsumos(Gasto g)
    {
        decimal consumido = presRepo.ObtenerConsumido(g.Id, null);
        if (consumido <= 0) return;

        if (!g.EsSobre)
            throw new ArgumentException(
                $"Este egreso ya tiene ${consumido:N2} en consumos registrados. Elimina esos consumos antes de dejar de tratarlo como sobre.");

        if (g.Monto < consumido)
            throw new ArgumentException(
                $"Ya consumiste ${consumido:N2} de este sobre, asi que no puedes bajarlo a ${g.Monto:N2}.");
    }
}
