using System;

namespace GestorIngresosEgresos.Modelo
{
    public class Gasto
    {
        public int Id { get; set; }
        public int PeriodoId { get; set; }
        public int? CategoriaId { get; set; }
        public int? DeudaId { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
        public string Descripcion { get; set; }

        // Cargado por JOIN
        public string CategoriaNombre { get; set; }

        public bool EsAbono => DeudaId.HasValue;
    }
}
