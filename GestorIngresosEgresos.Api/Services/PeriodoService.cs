using System.Globalization;
using GestorIngresosEgresos.Api.Modelo;
using GestorIngresosEgresos.Api.Repository;

namespace GestorIngresosEgresos.Api.Services;

public class PeriodoService(PeriodoRepository repo)
{
    public List<Periodo> ObtenerTodos(int usuarioId) => repo.ObtenerTodos(usuarioId);

    // Retorna el periodo del mes. Si es el mes actual y no existe, lo crea.
    public Periodo? ObtenerOCrearPeriodo(int usuarioId, int anio, int mes)
    {
        var p = repo.ObtenerPorMes(usuarioId, anio, mes);
        if (p != null) return p;

        bool esMesActual = anio == DateTime.Now.Year && mes == DateTime.Now.Month;
        if (!esMesActual) return null;

        var fechaInicio = new DateTime(anio, mes, 1);
        var fechaFin = fechaInicio.AddMonths(1).AddDays(-1);
        string nombre = fechaInicio.ToString("MMMM yyyy", new CultureInfo("es-ES"));
        nombre = char.ToUpper(nombre[0]) + nombre[1..];

        return repo.Guardar(new Periodo
        {
            UsuarioId = usuarioId,
            Nombre = nombre,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            SueldoBase = 0,
            SaldoInicial = 0,
            Estado = EstadoPeriodo.ABIERTO
        });
    }

    public Periodo? ObtenerPorId(int usuarioId, int id) => repo.ObtenerPorId(usuarioId, id);

    public void ActualizarSueldoBase(int usuarioId, int periodoId, decimal sueldoBase)
    {
        if (sueldoBase < 0) throw new ArgumentException("El sueldo base no puede ser negativo.");
        repo.ActualizarSueldoBase(usuarioId, periodoId, sueldoBase);
    }

    public void CerrarPeriodo(int usuarioId, int periodoId) => repo.Cerrar(usuarioId, periodoId);
}
