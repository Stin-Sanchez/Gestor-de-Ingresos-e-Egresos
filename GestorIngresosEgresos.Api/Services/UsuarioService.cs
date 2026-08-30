using System.Text.RegularExpressions;
using GestorIngresosEgresos.Api.Modelo;
using GestorIngresosEgresos.Api.Repository;

namespace GestorIngresosEgresos.Api.Services;

public partial class UsuarioService(UsuarioRepository repo, TotpService totp, AvatarService avatares)
{
    [GeneratedRegex(@"^[a-zA-Z0-9._-]{3,50}$")] private static partial Regex UsernameValido();
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")] private static partial Regex EmailValido();

    // Verifica con BCrypt; si el usuario aun tiene el hash legacy SHA-256, lo valida con
    // ese algoritmo y, si coincide, lo re-hashea a BCrypt transparentemente (sin downtime
    // para cuentas migradas desde la app de escritorio).
    public Usuario? VerificarCredenciales(string username, string password)
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

    public Usuario Registrar(string username, string password, string? email)
    {
        username = (username ?? "").Trim();
        email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

        if (!UsernameValido().IsMatch(username))
            throw new ArgumentException("El usuario debe tener entre 3 y 50 caracteres (letras, numeros, punto, guion o guion bajo).");
        ValidarPassword(password);
        if (email is not null && !EmailValido().IsMatch(email))
            throw new ArgumentException("El correo no tiene un formato valido.");
        if (repo.ExisteUsername(username))
            throw new InvalidOperationException("Ese nombre de usuario ya esta ocupado.");
        if (email is not null && repo.ExisteEmail(email))
            throw new InvalidOperationException("Ese correo ya esta registrado.");

        return repo.Crear(new Usuario
        {
            Username = username,
            PasswordHash = PasswordHelper.HashBCrypt(password),
            Email = email
        });
    }

    public Usuario Obtener(int usuarioId) =>
        repo.ObtenerPorId(usuarioId) ?? throw new KeyNotFoundException("Usuario no encontrado.");

    public void ActualizarPerfil(int usuarioId, string? email)
    {
        email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        if (email is not null && !EmailValido().IsMatch(email))
            throw new ArgumentException("El correo no tiene un formato valido.");
        if (email is not null && repo.ExisteEmail(email, usuarioId))
            throw new InvalidOperationException("Ese correo ya esta registrado por otra cuenta.");
        repo.ActualizarPerfil(usuarioId, email);
    }

    public void CambiarPassword(int usuarioId, string actual, string nueva)
    {
        var usuario = Obtener(usuarioId);
        if (VerificarCredenciales(usuario.Username, actual) is null)
            throw new ArgumentException("La contraseña actual no es correcta.");
        ValidarPassword(nueva);
        repo.ActualizarPasswordHash(usuarioId, PasswordHelper.HashBCrypt(nueva));
    }

    public async Task<string> CambiarAvatarAsync(int usuarioId, IFormFile archivo)
    {
        var usuario = Obtener(usuarioId);
        string nombre = await avatares.GuardarAsync(usuarioId, archivo);
        repo.ActualizarAvatar(usuarioId, nombre);
        avatares.Eliminar(usuario.Avatar); // el anterior ya no se referencia
        return nombre;
    }

    public void QuitarAvatar(int usuarioId)
    {
        var usuario = Obtener(usuarioId);
        repo.ActualizarAvatar(usuarioId, null);
        avatares.Eliminar(usuario.Avatar);
    }

    // ── Segundo factor ──────────────────────────────────────────────────
    public AltaTotp IniciarAltaTotp(int usuarioId)
    {
        var usuario = Obtener(usuarioId);
        if (usuario.TotpActivo)
            throw new InvalidOperationException("El segundo factor ya esta activo.");

        var alta = totp.GenerarAlta(usuario.Username);
        repo.ActualizarTotp(usuarioId, alta.Secret, activo: false);
        return alta;
    }

    public void ConfirmarAltaTotp(int usuarioId, string codigo)
    {
        var usuario = Obtener(usuarioId);
        if (usuario.TotpActivo)
            throw new InvalidOperationException("El segundo factor ya esta activo.");
        if (usuario.TotpSecret is null)
            throw new InvalidOperationException("Primero escanea el codigo QR.");
        if (!totp.Verificar(usuario.TotpSecret, codigo))
            throw new ArgumentException("El codigo no es valido. Revisa que la hora del telefono este sincronizada.");

        repo.ActualizarTotp(usuarioId, usuario.TotpSecret, activo: true);
    }

    // Pide la contraseña ademas del codigo: desactivar el segundo factor debilita
    // la cuenta, asi que no debe bastar con tener la sesion abierta.
    public void DesactivarTotp(int usuarioId, string password, string codigo)
    {
        var usuario = Obtener(usuarioId);
        if (!usuario.TotpActivo)
            throw new InvalidOperationException("El segundo factor no esta activo.");
        if (VerificarCredenciales(usuario.Username, password) is null)
            throw new ArgumentException("La contraseña no es correcta.");
        if (!totp.Verificar(usuario.TotpSecret, codigo))
            throw new ArgumentException("El codigo no es valido.");

        repo.ActualizarTotp(usuarioId, null, activo: false);
    }

    public bool VerificarCodigoTotp(Usuario usuario, string codigo) => totp.Verificar(usuario.TotpSecret, codigo);

    private static void ValidarPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.");
    }
}
