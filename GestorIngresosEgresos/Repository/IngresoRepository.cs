using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Util;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace GestorIngresosEgresos.Repository
{
    public class IngresoRepository
    {
        private readonly MySqlConnection _conn;

        public IngresoRepository()
        {
            _conn = ConexionDB.GetInstance().GetConnection();
        }

        public List<Ingreso> ObtenerPorPeriodo(int periodoId)
        {
            var lista = new List<Ingreso>();
            string sql = "SELECT * FROM ingresos WHERE periodo_id = @pid ORDER BY fecha DESC";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@pid", periodoId);
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) lista.Add(Mapear(r));
            }
            return lista;
        }

        public Ingreso ObtenerPorId(int id)
        {
            string sql = "SELECT * FROM ingresos WHERE id = @id";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? Mapear(r) : null;
            }
        }

        public Ingreso Guardar(Ingreso ing)
        {
            string sql = @"INSERT INTO ingresos (periodo_id, monto, fecha, descripcion, tipo)
                           VALUES (@pid, @monto, @fecha, @desc, @tipo);
                           SELECT LAST_INSERT_ID();";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@pid",   ing.PeriodoId);
                cmd.Parameters.AddWithValue("@monto", ing.Monto);
                cmd.Parameters.AddWithValue("@fecha", ing.Fecha.Date);
                cmd.Parameters.AddWithValue("@desc",  ing.Descripcion ?? "");
                cmd.Parameters.AddWithValue("@tipo",  ing.Tipo.ToString());
                ing.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
            return ing;
        }

        public void Actualizar(Ingreso ing)
        {
            string sql = "UPDATE ingresos SET monto=@monto, fecha=@fecha, descripcion=@desc, tipo=@tipo WHERE id=@id";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@monto", ing.Monto);
                cmd.Parameters.AddWithValue("@fecha", ing.Fecha.Date);
                cmd.Parameters.AddWithValue("@desc",  ing.Descripcion ?? "");
                cmd.Parameters.AddWithValue("@tipo",  ing.Tipo.ToString());
                cmd.Parameters.AddWithValue("@id",    ing.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public void Eliminar(int id)
        {
            string sql = "DELETE FROM ingresos WHERE id = @id";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private Ingreso Mapear(MySqlDataReader r) => new Ingreso
        {
            Id          = r.GetInt32("id"),
            PeriodoId   = r.GetInt32("periodo_id"),
            Monto       = r.GetDecimal("monto"),
            Fecha       = r.GetDateTime("fecha"),
            Descripcion = r.GetString("descripcion"),
            Tipo        = (TipoIngreso)Enum.Parse(typeof(TipoIngreso), r.GetString("tipo"))
        };
    }
}
