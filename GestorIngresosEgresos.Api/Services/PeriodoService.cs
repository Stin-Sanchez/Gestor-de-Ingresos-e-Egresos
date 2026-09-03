using System.Globalization;
using GestorIngresosEgresos.Api.Modelo;
using GestorIngresosEgresos.Api.Repository;

namespace GestorIngresosEgresos.Api.Services;

public record ConfigPeriodos(int DiaCorte, int DiasGracia);

public class PeriodoService(PeriodoRepository repo, UsuarioRepository usuarioRepo)
{
    public List<Periodo> ObtenerTodos(int usuarioId)
    {
        var cfg = ObtenerConfig(usuarioId);
        repo.CerrarVencidos(usuarioId, cfg.DiasGracia);
        return repo.ObtenerTodos(usuarioId);
    }

    public ConfigPeriodos ObtenerConfig(int usuarioId)
    {
        var u = usuarioRepo.ObtenerPorId(usuarioId) ?? throw new KeyNotFoundException("Usuario no encontrado.");
        return new ConfigPeriodos(u.DiaCorte, u.DiasGracia);
    }

    // Cambiar el corte solo afecta a los periodos que se creen despues: mover las fechas
    // de los que ya tienen movimientos dejaria gastos fuera del rango de su propio periodo.
    public void GuardarConfig(int usuarioId, int diaCorte, int diasGracia)
    {
        if (diaCorte is < 1 or > 31)
            throw new ArgumentException("El dia de corte debe estar entre 1 y 31.");
        if (diasGracia is < 0 or > 28)
            throw new ArgumentException("Los dias de gracia deben estar entre 0 y 28.");
        usuarioRepo.ActualizarConfigPeriodos(usuarioId, diaCorte, diasGracia);
    }

    // No hay scheduler: el cierre se evalua cuando el usuario entra. Es suficiente porque
    // lo unico que el estado gobierna son las escrituras, que solo ocurren estando dentro.
    public int CerrarVencidos(int usuarioId) => repo.CerrarVencidos(usuarioId, ObtenerConfig(usuarioId).DiasGracia);

    // Retorna el periodo del mes. Si es el periodo vigente y no existe, lo crea.
    public Periodo? ObtenerOCrearPeriodo(int usuarioId, int anio, int mes)
    {
        var cfg = ObtenerConfig(usuarioId);
        repo.CerrarVencidos(usuarioId, cfg.DiasGracia);

        var p = repo.ObtenerPorMes(usuarioId, anio, mes);
        if (p != null) return p;

        if ((anio, mes) != Periodo.MesNatural(DateTime.Today, cfg.DiaCorte)) return null;

        var (fechaInicio, fechaFin) = Periodo.RangoDe(anio, mes, cfg.DiaCorte);
        string nombre = fechaInicio.ToString("MMMM yyyy", new CultureInfo("es-ES"));
        nombre = char.ToUpper(nombre[0]) + nombre[1..];

        return repo.Guardar(new Periodo
        {
            UsuarioId = usuarioId,
            Nombre = nombre,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            SaldoInicial = 0,
            Estado = EstadoPeriodo.ABIERTO
        });
    }

    public Periodo? ObtenerPorId(int usuarioId, int id) => repo.ObtenerPorId(usuarioId, id);

    public void CerrarPeriodo(int usuarioId, int periodoId) => repo.Cerrar(usuarioId, periodoId);

    public void ReabrirPeriodo(int usuarioId, int periodoId)
    {
        var p = repo.ObtenerPorId(usuarioId, periodoId) ?? throw new KeyNotFoundException("Periodo no encontrado.");
        if (p.Abierto) throw new InvalidOperationException("El periodo ya esta abierto.");
        repo.Reabrir(usuarioId, periodoId);
    }

    // Unica puerta de escritura sobre un periodo. Todos los servicios que graban algo
    // ligado a un periodo pasan por aqui, para que ninguno se quede sin la validacion.
    public Periodo ExigirAbierto(int usuarioId, int periodoId)
    {
        var p = repo.ObtenerPorId(usuarioId, periodoId)
            ?? throw new KeyNotFoundException("No hay periodo para esta transaccion.");
        if (!p.Abierto)
            throw new InvalidOperationException(
                $"El periodo \"{p.Nombre}\" esta cerrado. Reabrelo desde Ajustes para poder registrar movimientos.");
        return p;
    }
}
