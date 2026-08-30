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

    // Un pago de deuda vive en gastos o en ingresos segun el tipo, asi que el historial
    // se expone con esta forma comun en vez de con la entidad de cada tabla.
    public record Movimiento(int Id, DateTime Fecha, decimal Monto, string Descripcion);

    public decimal SaldoPendiente => MontoOriginal - MontoPagado;
    public decimal PorcentajePagado => MontoOriginal > 0
        ? Math.Round((MontoPagado / MontoOriginal) * 100, 1)
        : 0;
}
