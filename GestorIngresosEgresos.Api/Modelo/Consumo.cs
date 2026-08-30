namespace GestorIngresosEgresos.Api.Modelo;

// Un consumo descuenta del sobre (Gasto con EsSobre = true) al que pertenece.
public class Consumo
{
    public int Id { get; set; }
    public int GastoId { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public string Descripcion { get; set; } = "";
}
