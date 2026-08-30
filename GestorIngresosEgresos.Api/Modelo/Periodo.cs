namespace GestorIngresosEgresos.Api.Modelo;

public enum EstadoPeriodo { ABIERTO, CERRADO }

public class Periodo
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = "";
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public decimal SaldoInicial { get; set; }
    public EstadoPeriodo Estado { get; set; }

    public bool EsActual =>
        FechaInicio.Year == DateTime.Now.Year &&
        FechaInicio.Month == DateTime.Now.Month;
}
