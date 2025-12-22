using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestorIngresosEgresos.Modelo
{

    public enum TipoMovimiento {
        INGRESO,
        EGRESO
    }

    public class Movimiento
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Nombre { get; set; }
        public decimal Monto { get; set; }
        public TipoMovimiento Tipo { get; set; }
        public string Descripcion { get; set; }
    }
}
