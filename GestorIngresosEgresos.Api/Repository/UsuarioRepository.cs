using GestorIngresosEgresos.Api.Data;
using GestorIngresosEgresos.Api.Modelo;
using MySql.Data.MySqlClient;

namespace GestorIngresosEgresos.Api.Repository;

public class UsuarioRepository(Db db)
{
    public Usuario? ObtenerPorUsername(string username)
    {
        const string sql = "SELECT id, username, password_hash FROM usuarios WHERE username = @u LIMIT 1";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@u", username);
        using var r = cmd.ExecuteReader();
        return r.Read()
            ? new Usuario { Id = r.GetInt32("id"), Username = r.GetString("username"), PasswordHash = r.GetString("password_hash") }
            : null;
    }

    public void ActualizarPasswordHash(int usuarioId, string nuevoHash)
    {
        const string sql = "UPDATE usuarios SET password_hash = @h WHERE id = @id";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@h", nuevoHash);
        cmd.Parameters.AddWithValue("@id", usuarioId);
        cmd.ExecuteNonQuery();
    }
}
