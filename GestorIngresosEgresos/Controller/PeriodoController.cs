using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Repository;
using System;
using System.Globalization;

namespace GestorIngresosEgresos.Controller
{
    public class PeriodoController
    {
        private readonly PeriodoRepository _repo;

        public PeriodoController()
        {
            _repo = new PeriodoRepository();
        }

        // Retorna el periodo del mes. Si es el mes actual y no existe, lo crea.
        public Periodo ObtenerOCrearPeriodo(int anio, int mes)
        {
            Periodo p = _repo.ObtenerPorMes(anio, mes);
            if (p != null) return p;

            bool esMesActual = anio == DateTime.Now.Year && mes == DateTime.Now.Month;
            if (!esMesActual) return null;

            var fechaInicio = new DateTime(anio, mes, 1);
            var fechaFin    = fechaInicio.AddMonths(1).AddDays(-1);
            string nombre   = fechaInicio.ToString("MMMM yyyy", new CultureInfo("es-ES"));
            nombre = char.ToUpper(nombre[0]) + nombre.Substring(1);

            return _repo.Guardar(new Periodo
            {
                Nombre       = nombre,
                FechaInicio  = fechaInicio,
                FechaFin     = fechaFin,
                SueldoBase   = 0,
                SaldoInicial = 0,
                Estado       = EstadoPeriodo.ABIERTO
            });
        }

        public void ActualizarSueldoBase(int periodoId, decimal sueldoBase)
        {
            if (sueldoBase < 0) throw new ArgumentException("El sueldo base no puede ser negativo.");
            _repo.ActualizarSueldoBase(periodoId, sueldoBase);
        }

        public void CerrarPeriodo(int periodoId)
        {
            _repo.Cerrar(periodoId);
        }
    }
}
