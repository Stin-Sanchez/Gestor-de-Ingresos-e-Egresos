using System;

namespace GestorIngresosEgresos.Modelo
{
    public class DeudaMovimiento
    {
        public int Id { get; set; }
        public int DeudaId { get; set; }
        public decimal Monto { get; set; }  // positivo = incremento, negativo = abono
        public string Nota { get; set; }
        public DateTime Fecha { get; set; }
    }
}
