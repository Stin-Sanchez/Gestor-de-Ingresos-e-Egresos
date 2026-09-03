using GestorIngresosEgresos.Api.Data;
using GestorIngresosEgresos.Api.Modelo;
using MySql.Data.MySqlClient;

namespace GestorIngresosEgresos.Api.Repository;

public class UsuarioRepository(Db db)
{
    private const string Campos = "id, username, password_hash, email, avatar, totp_secret, totp_activo, dia_corte, dias_gracia";

    public Usuario? ObtenerPorUsername(string username)
    {
        using var conn = db.Open();
        using var cmd = new MySqlCommand($"SELECT {Campos} FROM usuarios WHERE username = @u LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@u", username);
        using var r = cmd.ExecuteReader();
        return r.Read() ? Mapear(r) : null;
    }

    public Usuario? ObtenerPorId(int id)
    {
        using var conn = db.Open();
        using var cmd = new MySqlCommand($"SELECT {Campos} FROM usuarios WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        using var r = cmd.ExecuteReader();
        return r.Read() ? Mapear(r) : null;
    }

    public bool ExisteUsername(string username)
    {
        using var conn = db.Open();
        using var cmd = new MySqlCommand("SELECT 1 FROM usuarios WHERE username = @u LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@u", username);
        return cmd.ExecuteScalar() is not null;
    }

    public bool ExisteEmail(string email, int? exceptoUsuarioId = null)
    {
        using var conn = db.Open();
        using var cmd = new MySqlCommand(
            "SELECT 1 FROM usuarios WHERE email = @e AND (@id IS NULL OR id <> @id) LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@e", email);
        cmd.Parameters.AddWithValue("@id", (object?)exceptoUsuarioId ?? DBNull.Value);
        return cmd.ExecuteScalar() is not null;
    }

    public Usuario Crear(Usuario u)
    {
        const string sql = @"INSERT INTO usuarios (username, password_hash, email)
                             VALUES (@u, @h, @e);
                             SELECT LAST_INSERT_ID();";
        using var conn = db.Open();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@u", u.Username);
        cmd.Parameters.AddWithValue("@h", u.PasswordHash);
        cmd.Parameters.AddWithValue("@e", (object?)u.Email ?? DBNull.Value);
        u.Id = Convert.ToInt32(cmd.ExecuteScalar());
        return u;
    }

    public void ActualizarPasswordHash(int usuarioId, string nuevoHash)
    {
        using var conn = db.Open();
        using var cmd = new MySqlCommand("UPDATE usuarios SET password_hash = @h WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@h", nuevoHash);
        cmd.Parameters.AddWithValue("@id", usuarioId);
        cmd.ExecuteNonQuery();
    }

    public void ActualizarPerfil(int usuarioId, string? email)
    {
        using var conn = db.Open();
        using var cmd = new MySqlCommand("UPDATE usuarios SET email = @e WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@e", (object?)email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", usuarioId);
        cmd.ExecuteNonQuery();
    }

    public void ActualizarAvatar(int usuarioId, string? archivo)
    {
        using var conn = db.Open();
        using var cmd = new MySqlCommand("UPDATE usuarios SET avatar = @a WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@a", (object?)archivo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", usuarioId);
        cmd.ExecuteNonQuery();
    }

    // El secreto se guarda al iniciar el alta; totp_activo solo se enciende cuando
    // el usuario confirma un codigo valido generado con ese secreto.
    public void ActualizarTotp(int usuarioId, string? secret, bool activo)
    {
        using var conn = db.Open();
        using var cmd = new MySqlCommand("UPDATE usuarios SET totp_secret = @s, totp_activo = @a WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@s", (object?)secret ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@a", activo);
        cmd.Parameters.AddWithValue("@id", usuarioId);
        cmd.ExecuteNonQuery();
    }

    public void ActualizarConfigPeriodos(int usuarioId, int diaCorte, int diasGracia)
    {
        using var conn = db.Open();
        using var cmd = new MySqlCommand("UPDATE usuarios SET dia_corte = @c, dias_gracia = @g WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@c", diaCorte);
        cmd.Parameters.AddWithValue("@g", diasGracia);
        cmd.Parameters.AddWithValue("@id", usuarioId);
        cmd.ExecuteNonQuery();
    }

    private static Usuario Mapear(MySqlDataReader r)
    {
        int emailOrd = r.GetOrdinal("email");
        int avatarOrd = r.GetOrdinal("avatar");
        int secretOrd = r.GetOrdinal("totp_secret");
        return new Usuario
        {
            Id = r.GetInt32("id"),
            Username = r.GetString("username"),
            PasswordHash = r.GetString("password_hash"),
            Email = r.IsDBNull(emailOrd) ? null : r.GetString(emailOrd),
            Avatar = r.IsDBNull(avatarOrd) ? null : r.GetString(avatarOrd),
            TotpSecret = r.IsDBNull(secretOrd) ? null : r.GetString(secretOrd),
            TotpActivo = r.GetBoolean("totp_activo"),
            DiaCorte = r.GetInt32("dia_corte"),
            DiasGracia = r.GetInt32("dias_gracia")
        };
    }
}
