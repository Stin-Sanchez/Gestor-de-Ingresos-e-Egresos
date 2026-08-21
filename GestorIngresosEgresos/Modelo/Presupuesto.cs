namespace GestorIngresosEgresos.Modelo
{
    public class Presupuesto
    {
        public int Id { get; set; }
        public int PeriodoId { get; set; }
        public int CategoriaId { get; set; }
        public decimal Monto { get; set; }
    }
}
