using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Util;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace GestorIngresosEgresos.Repository
{
    public class DeudaRepository
    {
        private readonly MySqlConnection _conn;

        public DeudaRepository()
        {
            _conn = ConexionDB.GetInstance().GetConnection();
        }

        public List<Deuda> ObtenerTodas()
        {
            var lista = new List<Deuda>();
            string sql = "SELECT * FROM deudas ORDER BY estado ASC, fecha_inicio DESC";
            using (var cmd = new MySqlCommand(sql, _conn))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) lista.Add(Mapear(r));
            return lista;
        }

        public Deuda ObtenerPorId(int id)
        {
            string sql = "SELECT * FROM deudas WHERE id = @id";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? Mapear(r) : null;
            }
        }

        public Deuda Guardar(Deuda d)
        {
            string sql = @"INSERT INTO deudas (nombre, acreedor, monto_original, monto_pagado, fecha_inicio, fecha_vencimiento, estado, descripcion)
                           VALUES (@nombre, @acreedor, @montoOrig, 0, @fechaIni, @fechaVenc, 'ACTIVA', @desc);
                           SELECT LAST_INSERT_ID();";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@nombre",    d.Nombre);
                cmd.Parameters.AddWithValue("@acreedor",  d.Acreedor);
                cmd.Parameters.AddWithValue("@montoOrig", d.MontoOriginal);
                cmd.Parameters.AddWithValue("@fechaIni",  d.FechaInicio.Date);
                cmd.Parameters.AddWithValue("@fechaVenc", (object)d.FechaVencimiento ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@desc",      d.Descripcion ?? "");
                d.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
            return d;
        }

        public void Eliminar(int id)
        {
            string sql = "DELETE FROM deudas WHERE id = @id";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        // Transaccion: crea Gasto (abono) en el periodo activo y actualiza la deuda
        public Gasto RegistrarAbono(int deudaId, int periodoId, int? categoriaId, decimal monto, string descripcion)
        {
            MySqlTransaction tx = _conn.BeginTransaction();
            try
            {
                string sqlGasto = @"INSERT INTO gastos (periodo_id, categoria_id, deuda_id, monto, fecha, descripcion)
                                    VALUES (@pid, @cat, @did, @monto, @fecha, @desc);
                                    SELECT LAST_INSERT_ID();";
                Gasto gasto;
                using (var cmd = new MySqlCommand(sqlGasto, _conn, tx))
                {
                    cmd.Parameters.AddWithValue("@pid",   periodoId);
                    cmd.Parameters.AddWithValue("@cat",   (object)categoriaId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@did",   deudaId);
                    cmd.Parameters.AddWithValue("@monto", monto);
                    cmd.Parameters.AddWithValue("@fecha", DateTime.Today);
                    cmd.Parameters.AddWithValue("@desc",  descripcion ?? "");
                    int gastoId = Convert.ToInt32(cmd.ExecuteScalar());
                    gasto = new Gasto
                    {
                        Id = gastoId, PeriodoId = periodoId, DeudaId = deudaId,
                        Monto = monto, Fecha = DateTime.Today, Descripcion = descripcion
                    };
                }

                string sqlDeuda = @"UPDATE deudas
                                    SET monto_pagado = monto_pagado + @monto,
                                        estado = CASE WHEN monto_pagado >= monto_original THEN 'PAGADA' ELSE 'ACTIVA' END
                                    WHERE id = @did";
                using (var cmd = new MySqlCommand(sqlDeuda, _conn, tx))
                {
                    cmd.Parameters.AddWithValue("@monto", monto);
                    cmd.Parameters.AddWithValue("@did",   deudaId);
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

        private Deuda Mapear(MySqlDataReader r)
        {
            int vencOrd = r.GetOrdinal("fecha_vencimiento");
            return new Deuda
            {
                Id               = r.GetInt32("id"),
                Nombre           = r.GetString("nombre"),
                Acreedor         = r.GetString("acreedor"),
                MontoOriginal    = r.GetDecimal("monto_original"),
                MontoPagado      = r.GetDecimal("monto_pagado"),
                FechaInicio      = r.GetDateTime("fecha_inicio"),
                FechaVencimiento = r.IsDBNull(vencOrd) ? (DateTime?)null : r.GetDateTime(vencOrd),
                Estado           = (EstadoDeuda)Enum.Parse(typeof(EstadoDeuda), r.GetString("estado")),
                Descripcion      = r.GetString("descripcion")
            };
        }
    }
}
