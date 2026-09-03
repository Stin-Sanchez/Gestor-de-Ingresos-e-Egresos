namespace GestorIngresosEgresos.Api.Modelo;

public enum EstadoDeuda { ACTIVA, PAGADA }

// DEBO: saldarla resta del periodo (se registra como gasto).
// ME_DEBEN: cobrarla suma (se registra como ingreso).
public enum TipoDeuda { DEBO, ME_DEBEN }

public class Deuda
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public TipoDeuda Tipo { get; set; }
    public string Nombre { get; set; } = "";
    public string Acreedor { get; set; } = "";
    public decimal MontoOriginal { get; set; }
    public decimal MontoPagado { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public EstadoDeuda Estado { get; set; }
    public string Descripcion { get; set; } = "";

    // Un movimiento de deuda vive en gastos o en ingresos segun el tipo (los pagos) o en
    // deuda_ampliaciones (lo que se presto de mas), asi que el historial
    // se expone con esta forma comun en vez de con la entidad de cada tabla. EsAmpliacion
    // distingue el origen: un pago baja el saldo, una ampliacion lo sube.
    public record Movimiento(int Id, DateTime Fecha, decimal Monto, string Descripcion, bool EsAmpliacion = false);

    public decimal SaldoPendiente => MontoOriginal - MontoPagado;
    public decimal PorcentajePagado => MontoOriginal > 0
        ? Math.Round((MontoPagado / MontoOriginal) * 100, 1)
        : 0;

    // ── Reglas de correccion de montos ──────────────────────────────────
    // Editar la deuda, editar una ampliacion y borrar una ampliacion son la misma
    // operacion vista de tres formas: dejar monto_original en otro valor. Por eso la
    // validacion y el estado resultante viven aqui una sola vez, y no en cada endpoint.

    // Bajar el total por debajo de lo ya pagado dejaria la deuda reportando mas pagado
    // de lo que jamas se debio. Corregir eso pasa primero por borrar pagos.
    public static bool TotalAdmisible(decimal nuevoTotal, decimal montoPagado) =>
        nuevoTotal > 0 && nuevoTotal >= montoPagado;

    public static EstadoDeuda EstadoTras(decimal nuevoTotal, decimal montoPagado) =>
        montoPagado >= nuevoTotal ? EstadoDeuda.PAGADA : EstadoDeuda.ACTIVA;

    // ponytail: self-check en vez de un proyecto de tests aparte; correr con
    // "dotnet run -- --selftest".
    public static bool SelfCheck()
    {
        bool ok = true;
        void Check(bool cond, string msg)
        {
            if (!cond) { Console.WriteLine("FALLO: " + msg); ok = false; }
        }

        Check(TotalAdmisible(100m, 0m), "un total por encima de lo pagado se admite");
        Check(TotalAdmisible(100m, 100m), "bajar el total justo a lo ya pagado se admite: queda saldada");
        Check(!TotalAdmisible(99.99m, 100m), "un centavo por debajo de lo pagado se rechaza");
        Check(!TotalAdmisible(0m, 0m), "un total de cero se rechaza aunque no haya pagos");
        Check(!TotalAdmisible(-50m, 0m), "un total negativo se rechaza");

        Check(EstadoTras(100m, 100m) == EstadoDeuda.PAGADA, "pagado igual al total queda PAGADA");
        Check(EstadoTras(100m, 40m) == EstadoDeuda.ACTIVA, "pagado por debajo del total queda ACTIVA");
        // Ampliar una deuda ya saldada es justo lo que pasa al prestar otra vez.
        Check(EstadoTras(150m, 100m) == EstadoDeuda.ACTIVA, "ampliar una deuda saldada la reactiva");
        // Y borrar la ampliacion que la reactivo tiene que volver a saldarla.
        Check(EstadoTras(100m, 100m) == EstadoDeuda.PAGADA, "deshacer esa ampliacion la vuelve a saldar");

        var d = new Deuda { MontoOriginal = 150m, MontoPagado = 60m };
        Check(d.SaldoPendiente == 90m, "saldo pendiente = total - pagado");
        Check(d.PorcentajePagado == 40m, "porcentaje pagado sobre el total ampliado");

        Console.WriteLine(ok ? "OK: checks de deuda pasaron." : "Uno o mas checks de deuda fallaron.");
        return ok;
    }
}
