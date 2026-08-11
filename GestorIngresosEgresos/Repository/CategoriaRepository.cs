using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Util;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace GestorIngresosEgresos.Repository
{
    public class CategoriaRepository
    {
        private readonly MySqlConnection _conn;

        public CategoriaRepository()
        {
            _conn = ConexionDB.GetInstance().GetConnection();
        }

        public List<CategoriaGasto> ObtenerTodas()
        {
            var lista = new List<CategoriaGasto>();
            string sql = "SELECT * FROM categorias_gasto ORDER BY nombre";
            using (var cmd = new MySqlCommand(sql, _conn))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                    lista.Add(new CategoriaGasto { Id = r.GetInt32("id"), Nombre = r.GetString("nombre") });
            return lista;
        }
    }
}
