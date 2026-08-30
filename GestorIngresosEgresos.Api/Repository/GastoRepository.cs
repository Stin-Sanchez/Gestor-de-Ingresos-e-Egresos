using GestorIngresosEgresos.Api.Data;
using GestorIngresosEgresos.Api.Modelo;
using MySql.Data.MySqlClient;

namespace GestorIngresosEgresos.Api.Repository;

public class GastoRepository(Db db)
{
    public List<Gasto> ObtenerPorPeriodo(int usuarioId, int periodoId)
    {
        var lista = new List<Gasto>();
        const string sql = @"SELECT g.*, c.nombre AS cat_nombre
                           FROM gastos g
                           JOIN periodos p ON p.id = g.periodo_id
                           LEFT JOIN categorias_gasto c ON g.categoria_id = c.id
                           WHERE g.periodo_id = @pid AND p.usuario_id = @uid
                           ORDER BY g.fecha DESC";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@pid", periodoId);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        using var r = cmd.ExecuteReader();
        while (r.Read()) lista.Add(Mapear(r));
        return lista;
    }

    public Gasto? ObtenerPorId(int usuarioId, int id)
    {
        const string sql = @"SELECT g.*, c.nombre AS cat_nombre
                           FROM gastos g
                           JOIN periodos p ON p.id = g.periodo_id
                           LEFT JOIN categorias_gasto c ON g.categoria_id = c.id
                           WHERE g.id = @id AND p.usuario_id = @uid";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        using var r = cmd.ExecuteReader();
        return r.Read() ? Mapear(r) : null;
    }

    public Gasto Guardar(Gasto g)
    {
        const string sql = @"INSERT INTO gastos (periodo_id, categoria_id, deuda_id, monto, fecha, descripcion, es_sobre)
                           VALUES (@pid, @cat, @did, @monto, @fecha, @desc, @sobre);
                           SELECT LAST_INSERT_ID();";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@pid", g.PeriodoId);
        cmd.Parameters.AddWithValue("@cat", (object?)g.CategoriaId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@did", (object?)g.DeudaId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@monto", g.Monto);
        cmd.Parameters.AddWithValue("@fecha", g.Fecha.Date);
        cmd.Parameters.AddWithValue("@desc", g.Descripcion ?? "");
        cmd.Parameters.AddWithValue("@sobre", g.EsSobre);
        g.Id = Convert.ToInt32(cmd.ExecuteScalar());
        return g;
    }

    public void Actualizar(int usuarioId, Gasto g)
    {
        const string sql = @"UPDATE gastos ga JOIN periodos p ON p.id = ga.periodo_id
                              SET ga.categoria_id=@cat, ga.monto=@monto, ga.fecha=@fecha, ga.descripcion=@desc, ga.es_sobre=@sobre
                              WHERE ga.id=@id AND p.usuario_id=@uid";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@cat", (object?)g.CategoriaId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@monto", g.Monto);
        cmd.Parameters.AddWithValue("@fecha", g.Fecha.Date);
        cmd.Parameters.AddWithValue("@desc", g.Descripcion ?? "");
        cmd.Parameters.AddWithValue("@sobre", g.EsSobre);
        cmd.Parameters.AddWithValue("@id", g.Id);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        cmd.ExecuteNonQuery();
    }

    public void Eliminar(int usuarioId, int id)
    {
        const string sql = @"DELETE ga FROM gastos ga JOIN periodos p ON p.id = ga.periodo_id
                              WHERE ga.id = @id AND p.usuario_id = @uid";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        cmd.ExecuteNonQuery();
    }

    private static Gasto Mapear(MySqlDataReader r)
    {
        int catOrd = r.GetOrdinal("categoria_id");
        int deudaOrd = r.GetOrdinal("deuda_id");
        int catNomOrd = r.GetOrdinal("cat_nombre");
        return new Gasto
        {
            Id = r.GetInt32("id"),
            PeriodoId = r.GetInt32("periodo_id"),
            CategoriaId = r.IsDBNull(catOrd) ? null : r.GetInt32(catOrd),
            DeudaId = r.IsDBNull(deudaOrd) ? null : r.GetInt32(deudaOrd),
            Monto = r.GetDecimal("monto"),
            Fecha = r.GetDateTime("fecha"),
            Descripcion = r.GetString("descripcion"),
            EsSobre = r.GetBoolean("es_sobre"),
            CategoriaNombre = r.IsDBNull(catNomOrd) ? "" : r.GetString(catNomOrd)
        };
    }
}
