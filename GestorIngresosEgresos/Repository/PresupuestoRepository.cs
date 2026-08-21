using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Util;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace GestorIngresosEgresos.Repository
{
    public class PresupuestoRepository
    {
        private readonly MySqlConnection _conn;

        public PresupuestoRepository()
        {
            _conn = ConexionDB.GetInstance().GetConnection();
        }

        public List<PresupuestoResumen> ObtenerResumenPorPeriodo(int periodoId)
        {
            var lista = new List<PresupuestoResumen>();
            string sql = @"SELECT p.id, p.categoria_id, c.nombre AS categoria_nombre, p.monto AS limite,
                                  COALESCE(SUM(g.monto), 0) AS gastado
                           FROM presupuestos p
                           JOIN categorias_gasto c ON c.id = p.categoria_id
                           LEFT JOIN gastos g ON g.periodo_id = p.periodo_id AND g.categoria_id = p.categoria_id AND g.deuda_id IS NULL
                           WHERE p.periodo_id = @pid
                           GROUP BY p.id, p.categoria_id, c.nombre, p.monto
                           ORDER BY c.nombre";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@pid", periodoId);
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        lista.Add(new PresupuestoResumen
                        {
                            Id              = r.GetInt32("id"),
                            CategoriaId     = r.GetInt32("categoria_id"),
                            CategoriaNombre = r.GetString("categoria_nombre"),
                            Limite          = r.GetDecimal("limite"),
                            Gastado         = r.GetDecimal("gastado")
                        });
            }
            return lista;
        }

        public Presupuesto ObtenerPorCategoria(int periodoId, int categoriaId)
        {
            string sql = "SELECT * FROM presupuestos WHERE periodo_id = @pid AND categoria_id = @cat LIMIT 1";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@pid", periodoId);
                cmd.Parameters.AddWithValue("@cat", categoriaId);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new Presupuesto
                    {
                        Id          = r.GetInt32("id"),
                        PeriodoId   = r.GetInt32("periodo_id"),
                        CategoriaId = r.GetInt32("categoria_id"),
                        Monto       = r.GetDecimal("monto")
                    };
                }
            }
        }

        public decimal ObtenerGastado(int periodoId, int categoriaId, int? excludeGastoId)
        {
            string sql = @"SELECT COALESCE(SUM(monto), 0) FROM gastos
                           WHERE periodo_id = @pid AND categoria_id = @cat AND deuda_id IS NULL
                             AND (@exclude IS NULL OR id <> @exclude)";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@pid", periodoId);
                cmd.Parameters.AddWithValue("@cat", categoriaId);
                cmd.Parameters.AddWithValue("@exclude", (object)excludeGastoId ?? DBNull.Value);
                return Convert.ToDecimal(cmd.ExecuteScalar());
            }
        }

        public Presupuesto Guardar(Presupuesto p)
        {
            string sql = @"INSERT INTO presupuestos (periodo_id, categoria_id, monto) VALUES (@pid, @cat, @monto);
                           SELECT LAST_INSERT_ID();";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@pid",   p.PeriodoId);
                cmd.Parameters.AddWithValue("@cat",   p.CategoriaId);
                cmd.Parameters.AddWithValue("@monto", p.Monto);
                p.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
            return p;
        }

        public void Actualizar(Presupuesto p)
        {
            string sql = "UPDATE presupuestos SET monto = @monto WHERE id = @id";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@monto", p.Monto);
                cmd.Parameters.AddWithValue("@id",    p.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public void Eliminar(int id)
        {
            string sql = "DELETE FROM presupuestos WHERE id = @id";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
