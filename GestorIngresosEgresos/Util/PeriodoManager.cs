using System;
using System.Globalization;

namespace GestorIngresosEgresos.Util
{
    public static class PeriodoManager
    {
        private static int _mes = DateTime.Now.Month;
        private static int _anio = DateTime.Now.Year;

        public static int Mes => _mes;
        public static int Anio => _anio;

        public static string NombrePeriodo =>
            new DateTime(_anio, _mes, 1).ToString("MMMM yyyy", new CultureInfo("es-ES"));

        public static bool EsPeriodoActual =>
            _mes == DateTime.Now.Month && _anio == DateTime.Now.Year;

        public static void IrAnterior()
        {
            if (_mes == 1) { _mes = 12; _anio--; }
            else _mes--;
        }

        public static void IrSiguiente()
        {
            if (EsPeriodoActual) return;
            if (_mes == 12) { _mes = 1; _anio++; }
            else _mes++;
        }
    }
}
