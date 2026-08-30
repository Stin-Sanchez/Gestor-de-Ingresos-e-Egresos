using GestorIngresosEgresos.Api.Modelo;
using GestorIngresosEgresos.Api.Repository;

namespace GestorIngresosEgresos.Api.Services;

public class UsuarioService(UsuarioRepository repo)
{
    // Verifica con BCrypt; si el usuario aun tiene el hash legacy SHA-256, lo valida con
    // ese algoritmo y, si coincide, lo re-hashea a BCrypt transparentemente (sin downtime
    // para cuentas migradas desde la app de escritorio).
    public Usuario? Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        var usuario = repo.ObtenerPorUsername(username.Trim());
        if (usuario is null) return null;

        if (PasswordHelper.EsHashLegacy(usuario.PasswordHash))
        {
            if (PasswordHelper.HashLegacy(password) != usuario.PasswordHash) return null;
            usuario.PasswordHash = PasswordHelper.HashBCrypt(password);
            repo.ActualizarPasswordHash(usuario.Id, usuario.PasswordHash);
            return usuario;
        }

        return PasswordHelper.VerifyBCrypt(password, usuario.PasswordHash) ? usuario : null;
    }
}
