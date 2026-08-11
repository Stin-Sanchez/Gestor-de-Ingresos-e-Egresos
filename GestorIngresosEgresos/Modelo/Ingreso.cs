using System;

namespace GestorIngresosEgresos.Modelo
{
    public enum TipoIngreso { SUELDO, EXTRA, OTRO }

    public class Ingreso
    {
        public int Id { get; set; }
        public int PeriodoId { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
        public string Descripcion { get; set; }
        public TipoIngreso Tipo { get; set; }
    }
}
