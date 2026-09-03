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

    // El tipo no se toca: los pagos de una DEBO viven en gastos y los de una ME_DEBEN en
    // ingresos, asi que cambiarlo dejaria el historial colgado en la tabla equivocada.
    public void Actualizar(int usuarioId, Deuda d, EstadoDeuda estado)
    {
        const string sql = @"UPDATE deudas
                             SET nombre = @nombre, acreedor = @acreedor, monto_original = @monto,
                                 fecha_inicio = @ini, fecha_vencimiento = @venc,
                                 descripcion = @desc, estado = @estado
                             WHERE id = @id AND usuario_id = @uid";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@nombre", d.Nombre);
        cmd.Parameters.AddWithValue("@acreedor", d.Acreedor);
        cmd.Parameters.AddWithValue("@monto", d.MontoOriginal);
        cmd.Parameters.AddWithValue("@ini", d.FechaInicio.Date);
        cmd.Parameters.AddWithValue("@venc", (object?)d.FechaVencimiento ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@desc", d.Descripcion ?? "");
        cmd.Parameters.AddWithValue("@estado", estado.ToString());
        cmd.Parameters.AddWithValue("@id", d.Id);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        cmd.ExecuteNonQuery();
    }

    public record Ampliacion(int Id, int DeudaId, decimal Monto);

    public Ampliacion? ObtenerAmpliacion(int usuarioId, int ampliacionId)
    {
        const string sql = @"SELECT a.id, a.deuda_id, a.monto
                             FROM deuda_ampliaciones a
                             JOIN deudas d ON d.id = a.deuda_id
                             WHERE a.id = @id AND d.usuario_id = @uid";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", ampliacionId);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        using var r = cmd.ExecuteReader();
        return r.Read() ? new Ampliacion(r.GetInt32("id"), r.GetInt32("deuda_id"), r.GetDecimal("monto")) : null;
    }

    // Corregir o borrar una ampliacion mueve el total de la deuda por la diferencia. El
    // estado llega ya calculado por el servicio, que es quien conoce lo pagado.
    public void ActualizarAmpliacion(int ampliacionId, int deudaId, decimal nuevoMonto, DateTime fecha, string? descripcion, decimal nuevoTotal, EstadoDeuda estado)
    {
        using var conn = db.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            const string sqlAmp = "UPDATE deuda_ampliaciones SET monto = @monto, fecha = @fecha, descripcion = @desc WHERE id = @id";
            using (var cmd = new MySqlCommand(sqlAmp, conn, tx))
            {
                cmd.Parameters.AddWithValue("@monto", nuevoMonto);
                cmd.Parameters.AddWithValue("@fecha", fecha.Date);
                cmd.Parameters.AddWithValue("@desc", descripcion ?? "");
                cmd.Parameters.AddWithValue("@id", ampliacionId);
                cmd.ExecuteNonQuery();
            }

            AjustarTotal(conn, tx, deudaId, nuevoTotal, estado);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void EliminarAmpliacion(int ampliacionId, int deudaId, decimal nuevoTotal, EstadoDeuda estado)
    {
        using var conn = db.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            using (var cmd = new MySqlCommand("DELETE FROM deuda_ampliaciones WHERE id = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", ampliacionId);
                cmd.ExecuteNonQuery();
            }

            AjustarTotal(conn, tx, deudaId, nuevoTotal, estado);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static void AjustarTotal(MySqlConnection conn, MySqlTransaction tx, int deudaId, decimal nuevoTotal, EstadoDeuda estado)
    {
        using var cmd = new MySqlCommand("UPDATE deudas SET monto_original = @monto, estado = @estado WHERE id = @did", conn, tx);
        cmd.Parameters.AddWithValue("@monto", nuevoTotal);
        cmd.Parameters.AddWithValue("@estado", estado.ToString());
        cmd.Parameters.AddWithValue("@did", deudaId);
        cmd.ExecuteNonQuery();
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

            // MySQL evalua los SET de izquierda a derecha y los de la derecha ya ven el valor
            // nuevo, asi que aqui monto_pagado ya viene restado: volver a restarle @monto
            // dejaba ACTIVA una deuda que el borrado si terminaba de saldar.
            const string sqlDeuda = @"UPDATE deudas
                                SET monto_pagado = GREATEST(monto_pagado - @monto, 0),
                                    estado = CASE WHEN monto_pagado >= monto_original THEN 'PAGADA' ELSE 'ACTIVA' END
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

    // Prestar mas sobre una deuda que ya existe sube monto_original. El estado se calcula
    // antes de la suma a proposito: MySQL evalua los SET de izquierda a derecha y los de la
    // derecha ya ven el valor nuevo, asi que con el orden inverso compararia contra un
    // monto_original ya ampliado y sumaria @monto dos veces.
    public Deuda.Movimiento Ampliar(int deudaId, decimal monto, DateTime fecha, string? descripcion)
    {
        using var conn = db.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            int id;
            const string sqlAmpliacion = @"INSERT INTO deuda_ampliaciones (deuda_id, monto, fecha, descripcion)
                                           VALUES (@did, @monto, @fecha, @desc);
                                           SELECT LAST_INSERT_ID();";
            using (var cmd = new MySqlCommand(sqlAmpliacion, conn, tx))
            {
                cmd.Parameters.AddWithValue("@did", deudaId);
                cmd.Parameters.AddWithValue("@monto", monto);
                cmd.Parameters.AddWithValue("@fecha", fecha.Date);
                cmd.Parameters.AddWithValue("@desc", descripcion ?? "");
                id = Convert.ToInt32(cmd.ExecuteScalar());
            }

            const string sqlDeuda = @"UPDATE deudas
                                SET estado = CASE WHEN monto_pagado >= monto_original + @monto THEN 'PAGADA' ELSE 'ACTIVA' END,
                                    monto_original = monto_original + @monto
                                WHERE id = @did";
            using (var cmd = new MySqlCommand(sqlDeuda, conn, tx))
            {
                cmd.Parameters.AddWithValue("@monto", monto);
                cmd.Parameters.AddWithValue("@did", deudaId);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
            return new Deuda.Movimiento(id, fecha.Date, monto, descripcion ?? "", EsAmpliacion: true);
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // El historial junta los pagos (gastos o ingresos, segun el tipo de la deuda) con las
    // ampliaciones, que viven en su propia tabla porque no mueven ningun periodo.
    public List<Deuda.Movimiento> ObtenerMovimientos(int usuarioId, TipoDeuda tipo, int deudaId)
    {
        string tabla = tipo == TipoDeuda.DEBO ? "gastos" : "ingresos";
        string sql = $@"SELECT m.id, m.fecha, m.monto, m.descripcion, 0 AS es_ampliacion
                        FROM {tabla} m
                        JOIN periodos p ON p.id = m.periodo_id
                        WHERE m.deuda_id = @did AND p.usuario_id = @uid
                        UNION ALL
                        SELECT a.id, a.fecha, a.monto, a.descripcion, 1 AS es_ampliacion
                        FROM deuda_ampliaciones a
                        JOIN deudas d ON d.id = a.deuda_id
                        WHERE a.deuda_id = @did AND d.usuario_id = @uid
                        ORDER BY fecha DESC, id DESC";

        var lista = new List<Deuda.Movimiento>();
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@did", deudaId);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            lista.Add(new Deuda.Movimiento(
                r.GetInt32("id"), r.GetDateTime("fecha"), r.GetDecimal("monto"), r.GetString("descripcion"),
                // El literal del UNION no llega tipado como bool desde MySQL.
                EsAmpliacion: Convert.ToInt32(r["es_ampliacion"]) == 1));
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
