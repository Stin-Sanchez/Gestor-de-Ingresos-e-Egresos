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
        const string sql = @"INSERT INTO deudas (usuario_id, tipo, nombre, acreedor, monto_original, monto_pagado, fecha_inicio, fecha_vencimiento, estado, descripcion)
                           VALUES (@uid, @tipo, @nombre, @acreedor, @montoOrig, 0, @fechaIni, @fechaVenc, 'ACTIVA', @desc);
                           SELECT LAST_INSERT_ID();";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@uid", d.UsuarioId);
        cmd.Parameters.AddWithValue("@tipo", d.Tipo.ToString());
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

    // Transaccion: registra el pago en el periodo indicado y actualiza la deuda.
    // Una deuda que debo se salda con un gasto; una que me deben, con un ingreso,
    // para que el saldo del periodo se mueva en la direccion correcta.
    // El caller (DeudaService) ya valido que el periodo y la deuda son del mismo usuario.
    public Deuda.Movimiento RegistrarPago(TipoDeuda tipo, int deudaId, int periodoId, int? categoriaId, decimal monto, string? descripcion)
    {
        string sqlPago = tipo == TipoDeuda.DEBO
            ? @"INSERT INTO gastos (periodo_id, categoria_id, deuda_id, monto, fecha, descripcion)
                VALUES (@pid, @cat, @did, @monto, @fecha, @desc);
                SELECT LAST_INSERT_ID();"
            : @"INSERT INTO ingresos (periodo_id, deuda_id, monto, fecha, descripcion, tipo)
                VALUES (@pid, @did, @monto, @fecha, @desc, 'OTRO');
                SELECT LAST_INSERT_ID();";

        using var conn = db.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var hoy = DateTime.Today;
            int pagoId;
            using (var cmd = new MySqlCommand(sqlPago, conn, tx))
            {
                cmd.Parameters.AddWithValue("@pid", periodoId);
                cmd.Parameters.AddWithValue("@did", deudaId);
                cmd.Parameters.AddWithValue("@monto", monto);
                cmd.Parameters.AddWithValue("@fecha", hoy);
                cmd.Parameters.AddWithValue("@desc", descripcion ?? "");
                if (tipo == TipoDeuda.DEBO)
                    cmd.Parameters.AddWithValue("@cat", (object?)categoriaId ?? DBNull.Value);
                pagoId = Convert.ToInt32(cmd.ExecuteScalar());
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
            return new Deuda.Movimiento(pagoId, hoy, monto, descripcion ?? "");
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // Borrar el gasto/ingreso de un pago sin devolverle el monto a la deuda la dejaria
    // reportando mas pagado de lo que tiene. Ambas cosas van en la misma transaccion.
    public void EliminarPago(TipoDeuda tipo, int deudaId, int movimientoId, decimal monto)
    {
        string tabla = tipo == TipoDeuda.DEBO ? "gastos" : "ingresos";

        using var conn = db.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            using (var cmd = new MySqlCommand($"DELETE FROM {tabla} WHERE id = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", movimientoId);
                cmd.ExecuteNonQuery();
            }

            const string sqlDeuda = @"UPDATE deudas
                                SET monto_pagado = GREATEST(monto_pagado - @monto, 0),
                                    estado = CASE WHEN monto_pagado - @monto >= monto_original THEN 'PAGADA' ELSE 'ACTIVA' END
                                WHERE id = @did";
            using (var cmd = new MySqlCommand(sqlDeuda, conn, tx))
            {
                cmd.Parameters.AddWithValue("@monto", monto);
                cmd.Parameters.AddWithValue("@did", deudaId);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // El historial sale de gastos o de ingresos segun el tipo de la deuda.
    public List<Deuda.Movimiento> ObtenerPagos(int usuarioId, TipoDeuda tipo, int deudaId)
    {
        string tabla = tipo == TipoDeuda.DEBO ? "gastos" : "ingresos";
        string sql = $@"SELECT m.id, m.fecha, m.monto, m.descripcion
                        FROM {tabla} m
                        JOIN periodos p ON p.id = m.periodo_id
                        WHERE m.deuda_id = @did AND p.usuario_id = @uid
                        ORDER BY m.fecha DESC, m.id DESC";

        var lista = new List<Deuda.Movimiento>();
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@did", deudaId);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            lista.Add(new Deuda.Movimiento(r.GetInt32("id"), r.GetDateTime("fecha"), r.GetDecimal("monto"), r.GetString("descripcion")));
        return lista;
    }

    private static Deuda Mapear(MySqlDataReader r)
    {
        int vencOrd = r.GetOrdinal("fecha_vencimiento");
        return new Deuda
        {
            Id = r.GetInt32("id"),
            UsuarioId = r.GetInt32("usuario_id"),
            Tipo = (TipoDeuda)Enum.Parse(typeof(TipoDeuda), r.GetString("tipo")),
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
