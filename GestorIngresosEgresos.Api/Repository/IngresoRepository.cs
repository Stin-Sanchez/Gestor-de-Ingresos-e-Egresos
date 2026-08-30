using GestorIngresosEgresos.Api.Data;
using GestorIngresosEgresos.Api.Modelo;
using MySql.Data.MySqlClient;

namespace GestorIngresosEgresos.Api.Repository;

// Todas las mutaciones hacen JOIN con periodos para exigir usuario_id: un ingreso
// no tiene su propio usuario_id, pero su periodo si, y por ahi se aisla.
public class IngresoRepository(Db db)
{
    public List<Ingreso> ObtenerPorPeriodo(int usuarioId, int periodoId)
    {
        var lista = new List<Ingreso>();
        const string sql = @"SELECT i.* FROM ingresos i
                              JOIN periodos p ON p.id = i.periodo_id
                              WHERE i.periodo_id = @pid AND p.usuario_id = @uid
                              ORDER BY i.fecha DESC";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@pid", periodoId);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        using var r = cmd.ExecuteReader();
        while (r.Read()) lista.Add(Mapear(r));
        return lista;
    }

    public Ingreso? ObtenerPorId(int usuarioId, int id)
    {
        const string sql = @"SELECT i.* FROM ingresos i
                              JOIN periodos p ON p.id = i.periodo_id
                              WHERE i.id = @id AND p.usuario_id = @uid";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        using var r = cmd.ExecuteReader();
        return r.Read() ? Mapear(r) : null;
    }

    public Ingreso Guardar(Ingreso ing)
    {
        const string sql = @"INSERT INTO ingresos (periodo_id, monto, fecha, descripcion, tipo)
                           VALUES (@pid, @monto, @fecha, @desc, @tipo);
                           SELECT LAST_INSERT_ID();";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@pid", ing.PeriodoId);
        cmd.Parameters.AddWithValue("@monto", ing.Monto);
        cmd.Parameters.AddWithValue("@fecha", ing.Fecha.Date);
        cmd.Parameters.AddWithValue("@desc", ing.Descripcion ?? "");
        cmd.Parameters.AddWithValue("@tipo", ing.Tipo.ToString());
        ing.Id = Convert.ToInt32(cmd.ExecuteScalar());
        return ing;
    }

    public void Actualizar(int usuarioId, Ingreso ing)
    {
        const string sql = @"UPDATE ingresos i JOIN periodos p ON p.id = i.periodo_id
                              SET i.monto=@monto, i.fecha=@fecha, i.descripcion=@desc, i.tipo=@tipo
                              WHERE i.id=@id AND p.usuario_id=@uid";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@monto", ing.Monto);
        cmd.Parameters.AddWithValue("@fecha", ing.Fecha.Date);
        cmd.Parameters.AddWithValue("@desc", ing.Descripcion ?? "");
        cmd.Parameters.AddWithValue("@tipo", ing.Tipo.ToString());
        cmd.Parameters.AddWithValue("@id", ing.Id);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        cmd.ExecuteNonQuery();
    }

    public void Eliminar(int usuarioId, int id)
    {
        const string sql = @"DELETE i FROM ingresos i JOIN periodos p ON p.id = i.periodo_id
                              WHERE i.id = @id AND p.usuario_id = @uid";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        cmd.ExecuteNonQuery();
    }

    private static Ingreso Mapear(MySqlDataReader r) => new()
    {
        Id = r.GetInt32("id"),
        PeriodoId = r.GetInt32("periodo_id"),
        Monto = r.GetDecimal("monto"),
        Fecha = r.GetDateTime("fecha"),
        Descripcion = r.GetString("descripcion"),
        Tipo = (TipoIngreso)Enum.Parse(typeof(TipoIngreso), r.GetString("tipo"))
    };
}
