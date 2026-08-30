using GestorIngresosEgresos.Api.Data;
using GestorIngresosEgresos.Api.Modelo;
using MySql.Data.MySqlClient;

namespace GestorIngresosEgresos.Api.Repository;

public class PeriodoRepository(Db db)
{
    public List<Periodo> ObtenerTodos(int usuarioId)
    {
        var lista = new List<Periodo>();
        const string sql = "SELECT * FROM periodos WHERE usuario_id = @uid ORDER BY fecha_inicio DESC";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        using var r = cmd.ExecuteReader();
        while (r.Read()) lista.Add(Mapear(r));
        return lista;
    }

    public Periodo? ObtenerPorId(int usuarioId, int id)
    {
        const string sql = "SELECT * FROM periodos WHERE id = @id AND usuario_id = @uid";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        using var r = cmd.ExecuteReader();
        return r.Read() ? Mapear(r) : null;
    }

    public Periodo? ObtenerPorMes(int usuarioId, int anio, int mes)
    {
        const string sql = "SELECT * FROM periodos WHERE usuario_id = @uid AND YEAR(fecha_inicio)=@a AND MONTH(fecha_inicio)=@m LIMIT 1";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        cmd.Parameters.AddWithValue("@a", anio);
        cmd.Parameters.AddWithValue("@m", mes);
        using var r = cmd.ExecuteReader();
        return r.Read() ? Mapear(r) : null;
    }

    public Periodo Guardar(Periodo p)
    {
        const string sql = @"INSERT INTO periodos (usuario_id, nombre, fecha_inicio, fecha_fin, sueldo_base, saldo_inicial, estado)
                           VALUES (@uid, @nombre, @ini, @fin, @sueldo, @saldo, @estado);
                           SELECT LAST_INSERT_ID();";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@uid", p.UsuarioId);
        cmd.Parameters.AddWithValue("@nombre", p.Nombre);
        cmd.Parameters.AddWithValue("@ini", p.FechaInicio.Date);
        cmd.Parameters.AddWithValue("@fin", p.FechaFin.Date);
        cmd.Parameters.AddWithValue("@sueldo", p.SueldoBase);
        cmd.Parameters.AddWithValue("@saldo", p.SaldoInicial);
        cmd.Parameters.AddWithValue("@estado", p.Estado.ToString());
        p.Id = Convert.ToInt32(cmd.ExecuteScalar());
        return p;
    }

    public void ActualizarSueldoBase(int usuarioId, int id, decimal sueldoBase)
    {
        const string sql = "UPDATE periodos SET sueldo_base = @s WHERE id = @id AND usuario_id = @uid";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@s", sueldoBase);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        cmd.ExecuteNonQuery();
    }

    public void Cerrar(int usuarioId, int id)
    {
        const string sql = "UPDATE periodos SET estado = 'CERRADO' WHERE id = @id AND usuario_id = @uid";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        cmd.ExecuteNonQuery();
    }

    private static Periodo Mapear(MySqlDataReader r) => new()
    {
        Id = r.GetInt32("id"),
        UsuarioId = r.GetInt32("usuario_id"),
        Nombre = r.GetString("nombre"),
        FechaInicio = r.GetDateTime("fecha_inicio"),
        FechaFin = r.GetDateTime("fecha_fin"),
        SueldoBase = r.GetDecimal("sueldo_base"),
        SaldoInicial = r.GetDecimal("saldo_inicial"),
        Estado = (EstadoPeriodo)Enum.Parse(typeof(EstadoPeriodo), r.GetString("estado"))
    };
}
