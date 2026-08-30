namespace GestorIngresosEgresos.Api.Modelo;

public enum EstadoPresupuesto { OK, ALERTA, CRITICO, EXCEDIDO }

// Estado de consumo de un sobre. El sobre es un Gasto con EsSobre = true:
// Limite es el monto que separaste, Gastado es la suma de sus consumos.
public class PresupuestoResumen
{
    public int GastoId { get; set; }
    public string Titulo { get; set; } = "";
    public string CategoriaNombre { get; set; } = "";
    public decimal Limite { get; set; }
    public decimal Gastado { get; set; }

    public decimal Disponible => Limite - Gastado;

    public decimal Porcentaje => Limite <= 0 ? 0 : Math.Min(Gastado / Limite * 100m, 999m);

    // Piso, no redondeo: el porcentaje mostrado nunca debe anunciar una banda que todavia no se
    // alcanza. Con redondeo, 99.6% se mostraria como "100%" mientras Estado sigue en CRITICO y
    // aun queda saldo disponible. Math.Floor(x) >= 80 exactamente cuando x >= 80, asi que el
    // numero mostrado y el estado siempre coinciden.
    public decimal PorcentajeMostrado => Math.Floor(Porcentaje);

    public EstadoPresupuesto Estado =>
        Porcentaje >= 100 ? EstadoPresupuesto.EXCEDIDO :
        Porcentaje >= 80  ? EstadoPresupuesto.CRITICO :
        Porcentaje >= 50  ? EstadoPresupuesto.ALERTA :
                             EstadoPresupuesto.OK;

    // Regla de bloqueo, aislada como funcion pura para poder verificarla en un self-check sin base de datos.
    // Un gasto que consume exactamente lo que queda se permite (aterriza justo en 100%).
    public static bool Excede(decimal monto, decimal limite, decimal gastado) => monto > limite - gastado;

    // ponytail: self-check en vez de un proyecto de tests aparte; correr con
    // "dotnet run -- --selftest". Si se agrega logica no trivial nueva a este calculo,
    // agregar mas casos aqui en vez de crear un test project.
    public static bool SelfCheck()
    {
        bool ok = true;
        void Check(bool cond, string msg)
        {
            if (!cond) { Console.WriteLine("FALLO: " + msg); ok = false; }
        }

        var r0 = new PresupuestoResumen { Limite = 20m, Gastado = 0m };
        Check(r0.Porcentaje == 0m, "0% cuando no hay gasto");
        Check(r0.Estado == EstadoPresupuesto.OK, "estado OK en 0%");
        Check(r0.Disponible == 20m, "disponible = limite cuando no hay gasto");

        var r50 = new PresupuestoResumen { Limite = 20m, Gastado = 10m };
        Check(r50.Estado == EstadoPresupuesto.ALERTA, "50% es ALERTA");

        var r80 = new PresupuestoResumen { Limite = 20m, Gastado = 16m };
        Check(r80.Estado == EstadoPresupuesto.CRITICO, "80% es CRITICO");

        var r100 = new PresupuestoResumen { Limite = 20m, Gastado = 20m };
        Check(r100.Estado == EstadoPresupuesto.EXCEDIDO, "100% es EXCEDIDO");
        Check(r100.Disponible == 0m, "disponible = 0 al 100%");

        var r99 = new PresupuestoResumen { Limite = 20m, Gastado = 19.9m };
        Check(r99.Estado == EstadoPresupuesto.CRITICO, "99.5% sigue siendo CRITICO, no EXCEDIDO");
        Check(r99.PorcentajeMostrado == 99m, "99.5% se muestra como 99, no se redondea a 100");

        var rSinLimite = new PresupuestoResumen { Limite = 0m, Gastado = 5m };
        Check(rSinLimite.Porcentaje == 0m, "limite 0 no lanza division por cero, retorna 0%");

        Check(!Excede(monto: 10m, limite: 20m, gastado: 10m), "un gasto que consume exactamente lo que queda se permite");
        Check(Excede(monto: 10.01m, limite: 20m, gastado: 10m), "un centavo por encima del disponible se bloquea");

        Console.WriteLine(ok ? "OK: todos los checks pasaron." : "Uno o mas checks fallaron.");
        return ok;
    }
}
