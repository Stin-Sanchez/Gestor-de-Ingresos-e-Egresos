using System;

namespace GestorIngresosEgresos.Modelo
{
    public enum EstadoPresupuesto { OK, ALERTA, CRITICO, EXCEDIDO }

    public class PresupuestoResumen
    {
        public int Id { get; set; }
        public int CategoriaId { get; set; }
        public string CategoriaNombre { get; set; }
        public decimal Limite { get; set; }
        public decimal Gastado { get; set; }

        public decimal Disponible => Limite - Gastado;

        public decimal Porcentaje => Limite <= 0 ? 0 : Math.Min(Gastado / Limite * 100m, 999m);

        public EstadoPresupuesto Estado =>
            Porcentaje >= 100 ? EstadoPresupuesto.EXCEDIDO :
            Porcentaje >= 80  ? EstadoPresupuesto.CRITICO :
            Porcentaje >= 50  ? EstadoPresupuesto.ALERTA :
                                 EstadoPresupuesto.OK;

        // ponytail: self-check en vez de un proyecto de tests aparte (el proyecto no tiene ninguno);
        // correr con "GestorIngresosEgresos.exe --selftest". Si se agrega logica no trivial nueva a este
        // calculo, agregar mas casos aqui en vez de crear un test project.
        public static bool SelfCheck()
        {
            bool ok = true;
            Action<bool, string> check = (cond, msg) =>
            {
                if (!cond) { Console.WriteLine("FALLO: " + msg); ok = false; }
            };

            var r0 = new PresupuestoResumen { Limite = 20m, Gastado = 0m };
            check(r0.Porcentaje == 0m, "0% cuando no hay gasto");
            check(r0.Estado == EstadoPresupuesto.OK, "estado OK en 0%");
            check(r0.Disponible == 20m, "disponible = limite cuando no hay gasto");

            var r49 = new PresupuestoResumen { Limite = 20m, Gastado = 9.8m };
            check(r49.Porcentaje == 49m, "49% se calcula correctamente");
            check(r49.Estado == EstadoPresupuesto.OK, "49% sigue siendo OK");

            var r50 = new PresupuestoResumen { Limite = 20m, Gastado = 10m };
            check(r50.Porcentaje == 50m, "50% se calcula correctamente");
            check(r50.Estado == EstadoPresupuesto.ALERTA, "50% es ALERTA");

            var r80 = new PresupuestoResumen { Limite = 20m, Gastado = 16m };
            check(r80.Estado == EstadoPresupuesto.CRITICO, "80% es CRITICO");

            var r100 = new PresupuestoResumen { Limite = 20m, Gastado = 20m };
            check(r100.Estado == EstadoPresupuesto.EXCEDIDO, "100% es EXCEDIDO");
            check(r100.Disponible == 0m, "disponible = 0 al 100%");

            var r150 = new PresupuestoResumen { Limite = 20m, Gastado = 30m };
            check(r150.Estado == EstadoPresupuesto.EXCEDIDO, "150% sigue EXCEDIDO");
            check(r150.Disponible == -10m, "disponible negativo cuando se excede");

            var rSinLimite = new PresupuestoResumen { Limite = 0m, Gastado = 5m };
            check(rSinLimite.Porcentaje == 0m, "limite 0 no lanza division por cero, retorna 0%");

            Console.WriteLine(ok ? "OK: todos los checks pasaron." : "Uno o mas checks fallaron.");
            return ok;
        }
    }
}
