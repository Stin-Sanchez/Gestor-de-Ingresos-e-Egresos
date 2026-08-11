using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestorIngresosEgresos.Modelo
{
    public class PagoDeuda
    {
        public int Id { get; set; }
        public int DeudaId { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Monto { get; set; }
        public string Nota { get; set; }
    }
}