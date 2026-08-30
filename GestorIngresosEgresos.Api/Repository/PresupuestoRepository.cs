using GestorIngresosEgresos.Api.Data;
using GestorIngresosEgresos.Api.Modelo;
using MySql.Data.MySqlClient;

namespace GestorIngresosEgresos.Api.Repository;

// Acceso a datos de los sobres y sus consumos. El sobre es una fila de gastos
// con es_sobre = 1; lo consumido nunca se guarda, siempre se suma de consumos.
public class PresupuestoRepository(Db db)
{
    private const string SelectResumen = @"
        SELECT g.id, g.descripcion AS titulo, g.monto AS limite,
               COALESCE(c.nombre, '') AS categoria_nombre,
               COALESCE((SELECT SUM(co.monto) FROM consumos co WHERE co.gasto_id = g.id), 0) AS gastado
        FROM gastos g
        JOIN periodos p ON p.id = g.periodo_id
        LEFT JOIN categorias_gasto c ON c.id = g.categoria_id";

    public List<PresupuestoResumen> ObtenerSobresPorPeriodo(int usuarioId, int periodoId)
    {
        var lista = new List<PresupuestoResumen>();
        string sql = SelectResumen + @"
                     WHERE g.periodo_id = @pid AND p.usuario_id = @uid AND g.es_sobre = 1 AND g.deuda_id IS NULL
                     ORDER BY g.fecha DESC, g.id DESC";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@pid", periodoId);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        using var r = cmd.ExecuteReader();
        while (r.Read()) lista.Add(MapearResumen(r));
        return lista;
    }

    public PresupuestoResumen? ObtenerResumenPorGasto(int usuarioId, int gastoId)
    {
        string sql = SelectResumen + " WHERE g.id = @gid AND p.usuario_id = @uid LIMIT 1";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@gid", gastoId);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        using var r = cmd.ExecuteReader();
        return r.Read() ? MapearResumen(r) : null;
    }

    public List<Consumo> ObtenerConsumos(int usuarioId, int gastoId)
    {
        var lista = new List<Consumo>();
        const string sql = @"SELECT co.* FROM consumos co
                              JOIN gastos g ON g.id = co.gasto_id
                              JOIN periodos p ON p.id = g.periodo_id
                              WHERE co.gasto_id = @gid AND p.usuario_id = @uid
                              ORDER BY co.fecha DESC, co.id DESC";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@gid", gastoId);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            lista.Add(new Consumo
            {
                Id = r.GetInt32("id"),
                GastoId = r.GetInt32("gasto_id"),
                Monto = r.GetDecimal("monto"),
                Fecha = r.GetDateTime("fecha"),
                Descripcion = r.GetString("descripcion")
            });
        return lista;
    }

    // excludeConsumoId permite validar una edicion sin que el consumo se cuente contra si mismo.
    public decimal ObtenerConsumido(int gastoId, int? excludeConsumoId)
    {
        const string sql = @"SELECT COALESCE(SUM(monto), 0) FROM consumos
                           WHERE gasto_id = @gid AND (@exclude IS NULL OR id <> @exclude)";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@gid", gastoId);
        cmd.Parameters.AddWithValue("@exclude", (object?)excludeConsumoId ?? DBNull.Value);
        return Convert.ToDecimal(cmd.ExecuteScalar());
    }

    public Consumo Guardar(Consumo c)
    {
        const string sql = @"INSERT INTO consumos (gasto_id, monto, fecha, descripcion)
                           VALUES (@gid, @monto, @fecha, @desc);
                           SELECT LAST_INSERT_ID();";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@gid", c.GastoId);
        cmd.Parameters.AddWithValue("@monto", c.Monto);
        cmd.Parameters.AddWithValue("@fecha", c.Fecha.Date);
        cmd.Parameters.AddWithValue("@desc", c.Descripcion ?? "");
        c.Id = Convert.ToInt32(cmd.ExecuteScalar());
        return c;
    }

    public void Actualizar(int usuarioId, Consumo c)
    {
        const string sql = @"UPDATE consumos co
                              JOIN gastos g ON g.id = co.gasto_id JOIN periodos p ON p.id = g.periodo_id
                              SET co.monto = @monto, co.fecha = @fecha, co.descripcion = @desc
                              WHERE co.id = @id AND p.usuario_id = @uid";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@monto", c.Monto);
        cmd.Parameters.AddWithValue("@fecha", c.Fecha.Date);
        cmd.Parameters.AddWithValue("@desc", c.Descripcion ?? "");
        cmd.Parameters.AddWithValue("@id", c.Id);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        cmd.ExecuteNonQuery();
    }

    public void Eliminar(int usuarioId, int id)
    {
        const string sql = @"DELETE co FROM consumos co
                              JOIN gastos g ON g.id = co.gasto_id JOIN periodos p ON p.id = g.periodo_id
                              WHERE co.id = @id AND p.usuario_id = @uid";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        cmd.ExecuteNonQuery();
    }

    private static PresupuestoResumen MapearResumen(MySqlDataReader r) => new()
    {
        GastoId = r.GetInt32("id"),
        Titulo = r.GetString("titulo"),
        CategoriaNombre = r.GetString("categoria_nombre"),
        Limite = r.GetDecimal("limite"),
        Gastado = r.GetDecimal("gastado")
    };
}
