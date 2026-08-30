using GestorIngresosEgresos.Api.Modelo;
using GestorIngresosEgresos.Api.Repository;

namespace GestorIngresosEgresos.Api.Services;

public class IngresoService(IngresoRepository repo, PeriodoRepository periodoRepo, DeudaRepository deudaRepo)
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
        var actual = repo.ObtenerPorId(usuarioId, ing.Id)
            ?? throw new KeyNotFoundException("Ingreso no encontrado.");
        // Editar el monto aqui dejaria la deuda descuadrada: el saldo cobrado se lleva
        // en la deuda, no en el ingreso. Para corregir hay que borrarlo y volver a cobrar.
        if (actual.DeudaId.HasValue)
            throw new InvalidOperationException("Este ingreso es el cobro de una deuda. Edítalo desde la sección de Deudas.");
        if (ing.Monto <= 0) throw new ArgumentException("El monto debe ser mayor a cero.");
        repo.Actualizar(usuarioId, ing);
    }

    public void Eliminar(int usuarioId, int id)
    {
        var ing = repo.ObtenerPorId(usuarioId, id)
            ?? throw new KeyNotFoundException("Ingreso no encontrado.");

        // Borrar un cobro tiene que devolverle el monto a la deuda, o quedaria
        // reportando mas cobrado de lo que realmente entro.
        if (ing.DeudaId is int deudaId)
            deudaRepo.EliminarPago(TipoDeuda.ME_DEBEN, deudaId, ing.Id, ing.Monto);
        else
            repo.Eliminar(usuarioId, id);
    }
}
