using System.Security.Cryptography;
using System.Text;

namespace GestorIngresosEgresos.Api.Services;

public static class PasswordHelper
{
    // SHA-256 sin salt: como hasheaba la contraseña la app de escritorio. Se mantiene
    // solo para reconocer credenciales existentes y migrarlas a BCrypt en el primer login.
    public static string HashLegacy(string input)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(64);
        foreach (byte b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    public static string HashBCrypt(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public static bool VerifyBCrypt(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);

    // Un hash BCrypt siempre empieza con $2; el legacy SHA-256 es 64 hex chars sin '$'.
    public static bool EsHashLegacy(string hash) => !hash.StartsWith('$');
}
