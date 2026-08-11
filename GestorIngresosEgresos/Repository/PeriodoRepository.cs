using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Util;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace GestorIngresosEgresos.Repository
{
    public class PeriodoRepository
    {
        private readonly MySqlConnection _conn;

        public PeriodoRepository()
        {
            _conn = ConexionDB.GetInstance().GetConnection();
        }

        public List<Periodo> ObtenerTodos()
        {
            var lista = new List<Periodo>();
            string sql = "SELECT * FROM periodos ORDER BY fecha_inicio DESC";
            using (var cmd = new MySqlCommand(sql, _conn))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) lista.Add(Mapear(r));
            return lista;
        }

        public Periodo ObtenerPorId(int id)
        {
            string sql = "SELECT * FROM periodos WHERE id = @id";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? Mapear(r) : null;
            }
        }

        public Periodo ObtenerPorMes(int anio, int mes)
        {
            string sql = "SELECT * FROM periodos WHERE YEAR(fecha_inicio)=@a AND MONTH(fecha_inicio)=@m LIMIT 1";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@a", anio);
                cmd.Parameters.AddWithValue("@m", mes);
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? Mapear(r) : null;
            }
        }

        public Periodo Guardar(Periodo p)
        {
            string sql = @"INSERT INTO periodos (nombre, fecha_inicio, fecha_fin, sueldo_base, saldo_inicial, estado)
                           VALUES (@nombre, @ini, @fin, @sueldo, @saldo, @estado);
                           SELECT LAST_INSERT_ID();";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@nombre", p.Nombre);
                cmd.Parameters.AddWithValue("@ini",    p.FechaInicio.Date);
                cmd.Parameters.AddWithValue("@fin",    p.FechaFin.Date);
                cmd.Parameters.AddWithValue("@sueldo", p.SueldoBase);
                cmd.Parameters.AddWithValue("@saldo",  p.SaldoInicial);
                cmd.Parameters.AddWithValue("@estado", p.Estado.ToString());
                p.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
            return p;
        }

        public void ActualizarSueldoBase(int id, decimal sueldoBase)
        {
            string sql = "UPDATE periodos SET sueldo_base = @s WHERE id = @id";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@s",  sueldoBase);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public void Cerrar(int id)
        {
            string sql = "UPDATE periodos SET estado = 'CERRADO' WHERE id = @id";
            using (var cmd = new MySqlCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private Periodo Mapear(MySqlDataReader r) => new Periodo
        {
            Id           = r.GetInt32("id"),
            Nombre       = r.GetString("nombre"),
            FechaInicio  = r.GetDateTime("fecha_inicio"),
            FechaFin     = r.GetDateTime("fecha_fin"),
            SueldoBase   = r.GetDecimal("sueldo_base"),
            SaldoInicial = r.GetDecimal("saldo_inicial"),
            Estado       = (EstadoPeriodo)Enum.Parse(typeof(EstadoPeriodo), r.GetString("estado"))
        };
    }
}
