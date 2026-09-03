namespace GestorIngresosEgresos.Api.Modelo;

public enum EstadoPeriodo { ABIERTO, CERRADO }

public class Periodo
{
    public const int DiaCortePorDefecto = 1;
    public const int DiasGraciaPorDefecto = 5;

    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = "";
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public decimal SaldoInicial { get; set; }
    public EstadoPeriodo Estado { get; set; }

    // Un periodo reabierto a mano queda exento del cierre automatico: si no, la
    // siguiente carga de la app lo cerraria otra vez y la reapertura no serviria
    // de nada. Se vuelve a cerrar solo cuando el usuario lo cierra a mano.
    public bool Reabierto { get; set; }

    public bool Abierto => Estado == EstadoPeriodo.ABIERTO;

    // Con dia de corte distinto de 1 el periodo vigente puede haber empezado el mes
    // pasado (corte 25: el 3 de octubre sigues dentro del periodo que abrio el 25 de
    // septiembre), asi que comparar año+mes de fecha_inicio ya no sirve.
    public bool EsActual => DateTime.Today >= FechaInicio.Date && DateTime.Today <= FechaFin.Date;

    // ── Reglas puras ────────────────────────────────────────────────────
    // Sin base de datos ni reloj implicito para poder verificarlas en el self-check.

    // El dia de corte se capa a los dias del mes: con corte 31, febrero arranca el 28.
    public static DateTime InicioDe(int anio, int mes, int diaCorte) =>
        new(anio, mes, Math.Clamp(diaCorte, 1, DateTime.DaysInMonth(anio, mes)));

    // El fin es la vispera del siguiente inicio, no "inicio + 1 mes - 1 dia": con corte
    // 31 el capado de febrero dejaria un hueco del 28 al 30 de marzo sin periodo.
    public static (DateTime Inicio, DateTime Fin) RangoDe(int anio, int mes, int diaCorte)
    {
        var inicio = InicioDe(anio, mes, diaCorte);
        var sig = new DateTime(anio, mes, 1).AddMonths(1);
        return (inicio, InicioDe(sig.Year, sig.Month, diaCorte).AddDays(-1));
    }

    // A que (año, mes) pertenece hoy. Antes del dia de corte todavia estas en el mes anterior.
    public static (int Anio, int Mes) MesNatural(DateTime hoy, int diaCorte)
    {
        if (hoy.Date >= InicioDe(hoy.Year, hoy.Month, diaCorte)) return (hoy.Year, hoy.Month);
        var anterior = new DateTime(hoy.Year, hoy.Month, 1).AddMonths(-1);
        return (anterior.Year, anterior.Month);
    }

    public static bool DebeCerrarse(DateTime fechaFin, bool reabierto, int diasGracia, DateTime hoy) =>
        !reabierto && hoy.Date > fechaFin.Date.AddDays(diasGracia);

    // ponytail: self-check en vez de un proyecto de tests aparte; correr con
    // "dotnet run -- --selftest".
    public static bool SelfCheck()
    {
        bool ok = true;
        void Check(bool cond, string msg)
        {
            if (!cond) { Console.WriteLine("FALLO: " + msg); ok = false; }
        }

        var (i1, f1) = RangoDe(2026, 9, 1);
        Check(i1 == new DateTime(2026, 9, 1), "corte 1: arranca el dia 1");
        Check(f1 == new DateTime(2026, 9, 30), "corte 1: termina el ultimo dia del mes");

        var (i25, f25) = RangoDe(2026, 9, 25);
        Check(i25 == new DateTime(2026, 9, 25), "corte 25: arranca el 25");
        Check(f25 == new DateTime(2026, 10, 24), "corte 25: termina la vispera del siguiente corte");

        // Febrero no tiene 31: el corte se capa, y el periodo anterior tiene que llegar
        // pegado al siguiente inicio o quedarian dias sin periodo.
        var (iFeb, fFeb) = RangoDe(2026, 2, 31);
        var (iMar, _) = RangoDe(2026, 3, 31);
        Check(iFeb == new DateTime(2026, 2, 28), "corte 31 en febrero se capa al 28");
        Check(iMar == new DateTime(2026, 3, 31), "marzo si tiene 31");
        Check(fFeb.AddDays(1) == iMar, "sin hueco entre febrero y marzo con corte 31");

        Check(MesNatural(new DateTime(2026, 10, 3), 25) == (2026, 9), "corte 25: el 3 de octubre sigues en septiembre");
        Check(MesNatural(new DateTime(2026, 10, 25), 25) == (2026, 10), "corte 25: el 25 ya es octubre");
        Check(MesNatural(new DateTime(2026, 10, 3), 1) == (2026, 10), "corte 1: el 3 de octubre es octubre");
        Check(MesNatural(new DateTime(2026, 1, 3), 25) == (2025, 12), "corte 25 en enero retrocede a diciembre del año anterior");

        var fin = new DateTime(2026, 9, 30);
        Check(!DebeCerrarse(fin, false, 5, new DateTime(2026, 10, 5)), "dentro de la gracia no se cierra");
        Check(!DebeCerrarse(fin, false, 5, new DateTime(2026, 10, 4)), "el ultimo dia de gracia no se cierra");
        Check(DebeCerrarse(fin, false, 5, new DateTime(2026, 10, 6)), "pasada la gracia se cierra");
        Check(!DebeCerrarse(fin, true, 5, new DateTime(2027, 1, 1)), "un periodo reabierto nunca se cierra solo");
        Check(DebeCerrarse(fin, false, 0, new DateTime(2026, 10, 1)), "sin gracia se cierra al dia siguiente del fin");

        Console.WriteLine(ok ? "OK: checks de periodo pasaron." : "Uno o mas checks de periodo fallaron.");
        return ok;
    }
}
