using System;

namespace GestorIngresosEgresos.Modelo
{
    public enum EstadoDeuda { ACTIVA, PAGADA }

    public class Deuda
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Acreedor { get; set; }
        public decimal MontoOriginal { get; set; }
        public decimal MontoPagado { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public EstadoDeuda Estado { get; set; }
        public string Descripcion { get; set; }

        public decimal SaldoPendiente => MontoOriginal - MontoPagado;
        public decimal PorcentajePagado => MontoOriginal > 0
            ? Math.Round((MontoPagado / MontoOriginal) * 100, 1)
            : 0;
    }
}
