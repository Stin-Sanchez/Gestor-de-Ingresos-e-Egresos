using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Util;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace GestorIngresosEgresos.Repository
{
    // Acceso a datos de los sobres y sus consumos. El sobre es una fila de gastos
    // con es_sobre = 1; lo consumido nunca se guarda, siempre se suma de consumos.
    public class PresupuestoRepository
    {
        private readonly MySqlConnection _conn;

        public PresupuestoRepository()
        {
            _conn = ConexionDB.GetInstance().GetConnection();
        }

        private const string SelectResumen = @"
            SELECT g.id, g.descripcion AS titulo, g.monto AS limite,
                   COALESCE(c.nombre, '') AS categoria_nombre,
                   COALESCE((SELECT SUM(co.monto) FROM consumos co WHERE co.gasto_id = g.id), 0) AS gastado
            FROM gastos g
            LEFT JOIN categorias_gasto c ON c.id = g.categoria_id";

        public List<PresupuestoResumen> ObtenerSobresPorPeriodo(int periodoId)
        {
            var lista = new List<PresupuestoResumen>();
            string sql = SelectResumen + @"
                         WHERE g.periodo_id = @pid AND g.es_sobre = 1 AND g.deuda_id IS NULL
                         ORDER BY g.fecha DESC, g.id DESC";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@pid", periodoId);
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) lista.Add(MapearResumen(r));
            }
            return lista;
        }

        public PresupuestoResumen ObtenerResumenPorGasto(int gastoId)
        {
            string sql = SelectResumen + " WHERE g.id = @gid LIMIT 1";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@gid", gastoId);
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? MapearResumen(r) : null;
            }
        }

        public List<Consumo> ObtenerConsumos(int gastoId)
        {
            var lista = new List<Consumo>();
            string sql = "SELECT * FROM consumos WHERE gasto_id = @gid ORDER BY fecha DESC, id DESC";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@gid", gastoId);
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        lista.Add(new Consumo
                        {
                            Id          = r.GetInt32("id"),
                            GastoId     = r.GetInt32("gasto_id"),
                            Monto       = r.GetDecimal("monto"),
                            Fecha       = r.GetDateTime("fecha"),
                            Descripcion = r.GetString("descripcion")
                        });
            }
            return lista;
        }

        // excludeConsumoId permite validar una edicion sin que el consumo se cuente contra si mismo.
        public decimal ObtenerConsumido(int gastoId, int? excludeConsumoId)
        {
            string sql = @"SELECT COALESCE(SUM(monto), 0) FROM consumos
                           WHERE gasto_id = @gid AND (@exclude IS NULL OR id <> @exclude)";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@gid", gastoId);
                cmd.Parameters.AddWithValue("@exclude", (object)excludeConsumoId ?? DBNull.Value);
                return Convert.ToDecimal(cmd.ExecuteScalar());
            }
        }

        public Consumo Guardar(Consumo c)
        {
            string sql = @"INSERT INTO consumos (gasto_id, monto, fecha, descripcion)
                           VALUES (@gid, @monto, @fecha, @desc);
                           SELECT LAST_INSERT_ID();";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@gid",   c.GastoId);
                cmd.Parameters.AddWithValue("@monto", c.Monto);
                cmd.Parameters.AddWithValue("@fecha", c.Fecha.Date);
                cmd.Parameters.AddWithValue("@desc",  c.Descripcion ?? "");
                c.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
            return c;
        }

        public void Actualizar(Consumo c)
        {
            string sql = "UPDATE consumos SET monto = @monto, fecha = @fecha, descripcion = @desc WHERE id = @id";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@monto", c.Monto);
                cmd.Parameters.AddWithValue("@fecha", c.Fecha.Date);
                cmd.Parameters.AddWithValue("@desc",  c.Descripcion ?? "");
                cmd.Parameters.AddWithValue("@id",    c.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public void Eliminar(int id)
        {
            string sql = "DELETE FROM consumos WHERE id = @id";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private PresupuestoResumen MapearResumen(MySqlDataReader r) => new PresupuestoResumen
        {
            GastoId         = r.GetInt32("id"),
            Titulo          = r.GetString("titulo"),
            CategoriaNombre = r.GetString("categoria_nombre"),
            Limite          = r.GetDecimal("limite"),
            Gastado         = r.GetDecimal("gastado")
        };
    }
}
