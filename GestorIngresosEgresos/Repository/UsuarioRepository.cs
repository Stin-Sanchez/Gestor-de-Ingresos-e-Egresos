using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Util;
using MySql.Data.MySqlClient;

namespace GestorIngresosEgresos.Repository
{
    public class UsuarioRepository
    {
        private readonly MySqlConnection connection;

        public UsuarioRepository()
        {
            connection = ConexionDB.GetInstance().GetConnection();
        }

        public Usuario ObtenerPorCredenciales(string username, string passwordHash)
        {
            const string query = "SELECT id, username FROM usuarios WHERE username = @u AND password_hash = @h LIMIT 1";
            MySqlCommand cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@h", passwordHash);

            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                    return new Usuario { Id = reader.GetInt32("id"), Username = reader.GetString("username") };
            }
            return null;
        }
    }
}
