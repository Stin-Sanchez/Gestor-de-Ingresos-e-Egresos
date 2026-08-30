using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Util;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace GestorIngresosEgresos.Repository
{
    public class GastoRepository
    {
        private readonly MySqlConnection _conn;

        public GastoRepository()
        {
            _conn = ConexionDB.GetInstance().GetConnection();
        }

        public List<Gasto> ObtenerPorPeriodo(int periodoId)
        {
            var lista = new List<Gasto>();
            string sql = @"SELECT g.*, c.nombre AS cat_nombre
                           FROM gastos g
                           LEFT JOIN categorias_gasto c ON g.categoria_id = c.id
                           WHERE g.periodo_id = @pid
                           ORDER BY g.fecha DESC";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@pid", periodoId);
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) lista.Add(Mapear(r));
            }
            return lista;
        }

        public List<Gasto> ObtenerAbonosPorDeuda(int deudaId)
        {
            var lista = new List<Gasto>();
            string sql = @"SELECT g.*, c.nombre AS cat_nombre
                           FROM gastos g
                           LEFT JOIN categorias_gasto c ON g.categoria_id = c.id
                           WHERE g.deuda_id = @did
                           ORDER BY g.fecha DESC";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@did", deudaId);
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) lista.Add(Mapear(r));
            }
            return lista;
        }

        public Gasto Guardar(Gasto g)
        {
            string sql = @"INSERT INTO gastos (periodo_id, categoria_id, deuda_id, monto, fecha, descripcion, es_sobre)
                           VALUES (@pid, @cat, @did, @monto, @fecha, @desc, @sobre);
                           SELECT LAST_INSERT_ID();";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@pid",   g.PeriodoId);
                cmd.Parameters.AddWithValue("@cat",   (object)g.CategoriaId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@did",   (object)g.DeudaId     ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@monto", g.Monto);
                cmd.Parameters.AddWithValue("@fecha", g.Fecha.Date);
                cmd.Parameters.AddWithValue("@desc",  g.Descripcion ?? "");
                cmd.Parameters.AddWithValue("@sobre", g.EsSobre);
                g.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
            return g;
        }

        public void Actualizar(Gasto g)
        {
            string sql = "UPDATE gastos SET categoria_id=@cat, monto=@monto, fecha=@fecha, descripcion=@desc, es_sobre=@sobre WHERE id=@id";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@cat",   (object)g.CategoriaId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@monto", g.Monto);
                cmd.Parameters.AddWithValue("@fecha", g.Fecha.Date);
                cmd.Parameters.AddWithValue("@desc",  g.Descripcion ?? "");
                cmd.Parameters.AddWithValue("@sobre", g.EsSobre);
                cmd.Parameters.AddWithValue("@id",    g.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public void Eliminar(int id)
        {
            string sql = "DELETE FROM gastos WHERE id = @id";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private Gasto Mapear(MySqlDataReader r)
        {
            int catOrd    = r.GetOrdinal("categoria_id");
            int deudaOrd  = r.GetOrdinal("deuda_id");
            int catNomOrd = r.GetOrdinal("cat_nombre");
            return new Gasto
            {
                Id              = r.GetInt32("id"),
                PeriodoId       = r.GetInt32("periodo_id"),
                CategoriaId     = r.IsDBNull(catOrd)    ? (int?)null : r.GetInt32(catOrd),
                DeudaId         = r.IsDBNull(deudaOrd)  ? (int?)null : r.GetInt32(deudaOrd),
                Monto           = r.GetDecimal("monto"),
                Fecha           = r.GetDateTime("fecha"),
                Descripcion     = r.GetString("descripcion"),
                EsSobre         = r.GetBoolean("es_sobre"),
                CategoriaNombre = r.IsDBNull(catNomOrd) ? "" : r.GetString(catNomOrd)
            };
        }
    }
}
