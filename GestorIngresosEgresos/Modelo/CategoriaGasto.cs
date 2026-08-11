namespace GestorIngresosEgresos.Modelo
{
    public class CategoriaGasto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }

        public override string ToString() => Nombre;
    }
}
