using GestorIngresosEgresos.Api.Data;
using GestorIngresosEgresos.Api.Modelo;
using MySql.Data.MySqlClient;

namespace GestorIngresosEgresos.Api.Repository;

public class DeudaRepository(Db db)
{
    public List<Deuda> ObtenerTodas(int usuarioId)
    {
        var lista = new List<Deuda>();
        const string sql = "SELECT * FROM deudas WHERE usuario_id = @uid ORDER BY estado ASC, fecha_inicio DESC";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        using var r = cmd.ExecuteReader();
        while (r.Read()) lista.Add(Mapear(r));
        return lista;
    }

    public Deuda? ObtenerPorId(int usuarioId, int id)
    {
        const string sql = "SELECT * FROM deudas WHERE id = @id AND usuario_id = @uid";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        using var r = cmd.ExecuteReader();
        return r.Read() ? Mapear(r) : null;
    }

    public Deuda Guardar(Deuda d)
    {
        const string sql = @"INSERT INTO deudas (usuario_id, nombre, acreedor, monto_original, monto_pagado, fecha_inicio, fecha_vencimiento, estado, descripcion)
                           VALUES (@uid, @nombre, @acreedor, @montoOrig, 0, @fechaIni, @fechaVenc, 'ACTIVA', @desc);
                           SELECT LAST_INSERT_ID();";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@uid", d.UsuarioId);
        cmd.Parameters.AddWithValue("@nombre", d.Nombre);
        cmd.Parameters.AddWithValue("@acreedor", d.Acreedor);
        cmd.Parameters.AddWithValue("@montoOrig", d.MontoOriginal);
        cmd.Parameters.AddWithValue("@fechaIni", d.FechaInicio.Date);
        cmd.Parameters.AddWithValue("@fechaVenc", (object?)d.FechaVencimiento ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@desc", d.Descripcion ?? "");
        d.Id = Convert.ToInt32(cmd.ExecuteScalar());
        return d;
    }

    public void Eliminar(int usuarioId, int id)
    {
        const string sql = "DELETE FROM deudas WHERE id = @id AND usuario_id = @uid";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        cmd.ExecuteNonQuery();
    }

    // Transaccion: crea Gasto (abono) en el periodo indicado y actualiza la deuda.
    // El caller (DeudaService) ya valido que el periodo y la deuda son del mismo usuario.
    public Gasto RegistrarAbono(int deudaId, int periodoId, int? categoriaId, decimal monto, string? descripcion)
    {
        using var conn = db.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            const string sqlGasto = @"INSERT INTO gastos (periodo_id, categoria_id, deuda_id, monto, fecha, descripcion)
                                VALUES (@pid, @cat, @did, @monto, @fecha, @desc);
                                SELECT LAST_INSERT_ID();";
            Gasto gasto;
            using (var cmd = new MySqlCommand(sqlGasto, conn, tx))
            {
                cmd.Parameters.AddWithValue("@pid", periodoId);
                cmd.Parameters.AddWithValue("@cat", (object?)categoriaId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@did", deudaId);
                cmd.Parameters.AddWithValue("@monto", monto);
                cmd.Parameters.AddWithValue("@fecha", DateTime.Today);
                cmd.Parameters.AddWithValue("@desc", descripcion ?? "");
                int gastoId = Convert.ToInt32(cmd.ExecuteScalar());
                gasto = new Gasto
                {
                    Id = gastoId,
                    PeriodoId = periodoId,
                    DeudaId = deudaId,
                    Monto = monto,
                    Fecha = DateTime.Today,
                    Descripcion = descripcion ?? ""
                };
            }

            const string sqlDeuda = @"UPDATE deudas
                                SET monto_pagado = monto_pagado + @monto,
                                    estado = CASE WHEN monto_pagado >= monto_original THEN 'PAGADA' ELSE 'ACTIVA' END
                                WHERE id = @did";
            using (var cmd = new MySqlCommand(sqlDeuda, conn, tx))
            {
                cmd.Parameters.AddWithValue("@monto", monto);
                cmd.Parameters.AddWithValue("@did", deudaId);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
            return gasto;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static Deuda Mapear(MySqlDataReader r)
    {
        int vencOrd = r.GetOrdinal("fecha_vencimiento");
        return new Deuda
        {
            Id = r.GetInt32("id"),
            UsuarioId = r.GetInt32("usuario_id"),
            Nombre = r.GetString("nombre"),
            Acreedor = r.GetString("acreedor"),
            MontoOriginal = r.GetDecimal("monto_original"),
            MontoPagado = r.GetDecimal("monto_pagado"),
            FechaInicio = r.GetDateTime("fecha_inicio"),
            FechaVencimiento = r.IsDBNull(vencOrd) ? null : r.GetDateTime(vencOrd),
            Estado = (EstadoDeuda)Enum.Parse(typeof(EstadoDeuda), r.GetString("estado")),
            Descripcion = r.GetString("descripcion")
        };
    }
}
