using GestorIngresosEgresos.Api.Modelo;
using GestorIngresosEgresos.Api.Repository;

namespace GestorIngresosEgresos.Api.Services;

public class GastoService(GastoRepository repo, PeriodoService periodos, PresupuestoRepository presRepo, CategoriaRepository catRepo, DeudaRepository deudaRepo)
{
    public List<Gasto> ObtenerPorPeriodo(int usuarioId, int periodoId) => repo.ObtenerPorPeriodo(usuarioId, periodoId);
    public List<CategoriaGasto> ObtenerCategorias() => catRepo.ObtenerTodas();

    public Gasto Guardar(int usuarioId, Gasto g)
    {
        periodos.ExigirAbierto(usuarioId, g.PeriodoId);
        if (g.Monto <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero.");
        if (string.IsNullOrWhiteSpace(g.Descripcion))
            throw new ArgumentException("La descripcion es obligatoria.");
        if (g.Fecha == default) g.Fecha = DateTime.Today;
        return repo.Guardar(g);
    }

    public void Actualizar(int usuarioId, Gasto g)
    {
        var actual = repo.ObtenerPorId(usuarioId, g.Id)
            ?? throw new KeyNotFoundException("Gasto no encontrado.");
        // El periodo del gasto, no el del body: si no, editar un gasto de un periodo
        // cerrado solo pediria mandar el id de uno abierto para saltarse el candado.
        periodos.ExigirAbierto(usuarioId, actual.PeriodoId);
        // Editar el monto aqui dejaria la deuda descuadrada: el saldo pagado se lleva
        // en la deuda, no en el gasto. Para corregir hay que borrarlo y volver a abonar.
        if (actual.DeudaId.HasValue)
            throw new InvalidOperationException("Este egreso es el abono de una deuda. Edítalo desde la sección de Deudas.");
        if (g.Monto <= 0) throw new ArgumentException("El monto debe ser mayor a cero.");
        if (string.IsNullOrWhiteSpace(g.Descripcion))
            throw new ArgumentException("La descripcion es obligatoria.");

        ValidarContraConsumos(g);
        repo.Actualizar(usuarioId, g);
    }

    public void Eliminar(int usuarioId, int id)
    {
        var g = repo.ObtenerPorId(usuarioId, id)
            ?? throw new KeyNotFoundException("Gasto no encontrado.");
        periodos.ExigirAbierto(usuarioId, g.PeriodoId);

        // Borrar un abono tiene que devolverle el monto a la deuda, o quedaria
        // reportando mas pagado de lo que realmente se pago.
        if (g.DeudaId is int deudaId)
            deudaRepo.EliminarPago(TipoDeuda.DEBO, deudaId, g.Id, g.Monto);
        else
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
