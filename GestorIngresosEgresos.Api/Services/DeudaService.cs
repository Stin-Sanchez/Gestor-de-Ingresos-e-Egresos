using GestorIngresosEgresos.Api.Modelo;
using GestorIngresosEgresos.Api.Repository;

namespace GestorIngresosEgresos.Api.Services;

// Totales separados por direccion: mezclar lo que debo con lo que me deben
// daria un numero que no significa nada.
public record ResumenDeudas(decimal Debo, decimal MeDeben, int ActivasDebo, int ActivasMeDeben)
{
    public decimal Neto => MeDeben - Debo;
}

public class DeudaService(DeudaRepository repo, PeriodoService periodos)
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

    // Corregir un dato mal tecleado no deberia costar borrar la deuda y su historial de
    // pagos. El tipo se conserva a proposito: los pagos de una DEBO estan en gastos y los
    // de una ME_DEBEN en ingresos, asi que darle la vuelta dejaria el historial huerfano.
    public void Actualizar(int usuarioId, Deuda d)
    {
        var actual = repo.ObtenerPorId(usuarioId, d.Id)
            ?? throw new KeyNotFoundException("Deuda no encontrada.");

        if (string.IsNullOrWhiteSpace(d.Nombre))
            throw new ArgumentException("El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(d.Acreedor))
            throw new ArgumentException(actual.Tipo == TipoDeuda.DEBO
                ? "El acreedor es obligatorio."
                : "Indica quien te debe.");
        if (d.FechaInicio == default) d.FechaInicio = actual.FechaInicio;

        ExigirTotalAdmisible(actual, d.MontoOriginal);
        repo.Actualizar(usuarioId, d, Deuda.EstadoTras(d.MontoOriginal, actual.MontoPagado));
    }

    public void ActualizarAmpliacion(int usuarioId, int ampliacionId, decimal monto, DateTime fecha, string? descripcion)
    {
        if (monto <= 0) throw new ArgumentException("El monto debe ser mayor a cero.");

        var (a, d) = AmpliacionCon(usuarioId, ampliacionId);
        if (fecha == default) fecha = DateTime.Today;

        // El total se mueve por la diferencia, no se recalcula: monto_original es la verdad
        // y las ampliaciones son el registro de lo que lo fue subiendo.
        decimal nuevoTotal = d.MontoOriginal - a.Monto + monto;
        ExigirTotalAdmisible(d, nuevoTotal);

        repo.ActualizarAmpliacion(a.Id, d.Id, monto, fecha, descripcion, nuevoTotal,
            Deuda.EstadoTras(nuevoTotal, d.MontoPagado));
    }

    public void EliminarAmpliacion(int usuarioId, int ampliacionId)
    {
        var (a, d) = AmpliacionCon(usuarioId, ampliacionId);

        decimal nuevoTotal = d.MontoOriginal - a.Monto;
        ExigirTotalAdmisible(d, nuevoTotal);

        repo.EliminarAmpliacion(a.Id, d.Id, nuevoTotal, Deuda.EstadoTras(nuevoTotal, d.MontoPagado));
    }

    private (DeudaRepository.Ampliacion, Deuda) AmpliacionCon(int usuarioId, int ampliacionId)
    {
        var a = repo.ObtenerAmpliacion(usuarioId, ampliacionId)
            ?? throw new KeyNotFoundException("Ampliacion no encontrada.");
        var d = repo.ObtenerPorId(usuarioId, a.DeudaId)
            ?? throw new KeyNotFoundException("Deuda no encontrada.");
        return (a, d);
    }

    // Unico sitio donde se decide si un total nuevo es valido: lo comparten editar la deuda,
    // editar una ampliacion y borrarla, que no son mas que tres formas de mover ese total.
    private static void ExigirTotalAdmisible(Deuda d, decimal nuevoTotal)
    {
        if (Deuda.TotalAdmisible(nuevoTotal, d.MontoPagado)) return;

        throw new ArgumentException(nuevoTotal <= 0
            ? "El monto debe ser mayor a cero."
            : $"La deuda quedaria en ${nuevoTotal:N2} y ya llevas ${d.MontoPagado:N2} pagados. Borra pagos antes de bajarla tanto.");
    }

    public void Eliminar(int usuarioId, int id) => repo.Eliminar(usuarioId, id);

    public List<Deuda.Movimiento> ObtenerMovimientos(int usuarioId, int deudaId)
    {
        var d = repo.ObtenerPorId(usuarioId, deudaId)
            ?? throw new KeyNotFoundException("Deuda no encontrada.");
        return repo.ObtenerMovimientos(usuarioId, d.Tipo, deudaId);
    }

    // Prestar mas a la misma persona amplia la deuda en vez de crear otra. No toca ningun
    // periodo, igual que crear una deuda: en este modelo el periodo solo se mueve al pagar.
    // Una deuda ya saldada vuelve a ACTIVA, que es justo lo que significa prestar de nuevo.
    public Deuda.Movimiento Ampliar(int usuarioId, int deudaId, decimal monto, DateTime fecha, string? descripcion)
    {
        if (monto <= 0) throw new ArgumentException("El monto debe ser mayor a cero.");

        var d = repo.ObtenerPorId(usuarioId, deudaId)
            ?? throw new KeyNotFoundException("Deuda no encontrada.");

        if (fecha == default) fecha = DateTime.Today;
        return repo.Ampliar(d.Id, monto, fecha, descripcion);
    }

    // Un abono (deuda que debo) se registra como gasto y baja el saldo del periodo;
    // un cobro (deuda que me deben) se registra como ingreso y lo sube.
    public Deuda.Movimiento RegistrarPago(int usuarioId, int deudaId, int periodoId, int? categoriaId, decimal monto, string? descripcion)
    {
        if (monto <= 0) throw new ArgumentException("El monto debe ser mayor a cero.");

        var d = repo.ObtenerPorId(usuarioId, deudaId)
            ?? throw new KeyNotFoundException("Deuda no encontrada.");
        periodos.ExigirAbierto(usuarioId, periodoId);

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
