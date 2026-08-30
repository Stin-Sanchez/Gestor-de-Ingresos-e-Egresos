using OtpNet;
using QRCoder;

namespace GestorIngresosEgresos.Api.Services;

public record AltaTotp(string Secret, string QrPngBase64);

// Segundo factor TOTP (RFC 6238), compatible con Google Authenticator, Authy, etc.
// Se apoya en Otp.NET en vez de implementar el algoritmo a mano: la validacion
// correcta depende de detalles finos (ventana de tolerancia al desfase de reloj,
// comparacion en tiempo constante) que no conviene reinventar en una ruta de login.
public class TotpService(IConfiguration config)
{
    private readonly string _emisor = config["Totp:Emisor"] ?? "Gestor Financiero";

    public AltaTotp GenerarAlta(string username)
    {
        string secret = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
        string uri = $"otpauth://totp/{Uri.EscapeDataString(_emisor)}:{Uri.EscapeDataString(username)}"
                   + $"?secret={secret}&issuer={Uri.EscapeDataString(_emisor)}&digits=6&period=30";

        using var generador = new QRCodeGenerator();
        using var datos = generador.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        // PngByteQRCode es 100% managed: no depende de System.Drawing, que en Linux
        // exige libgdiplus y romperia dentro del contenedor.
        byte[] png = new PngByteQRCode(datos).GetGraphic(8);

        return new AltaTotp(secret, Convert.ToBase64String(png));
    }

    public bool Verificar(string? secret, string? codigo)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(codigo)) return false;

        Totp totp;
        try
        {
            totp = new Totp(Base32Encoding.ToBytes(secret));
        }
        catch (ArgumentException)
        {
            return false; // secreto corrupto en la base: se trata como codigo invalido, no como error 500
        }

        // Tolera un paso de 30s hacia cada lado por desfase de reloj entre el telefono y el server.
        return totp.VerifyTotp(codigo.Trim().Replace(" ", ""), out _, VerificationWindow.RfcSpecifiedNetworkDelay);
    }

    // ponytail: self-check en vez de un proyecto de tests aparte, igual que
    // PresupuestoResumen. Correr con "dotnet run -- --selftest".
    public static bool SelfCheck()
    {
        bool ok = true;
        void Check(bool cond, string msg)
        {
            if (!cond) { Console.WriteLine("FALLO: " + msg); ok = false; }
        }

        var svc = new TotpService(new ConfigurationBuilder().Build());
        var alta = svc.GenerarAlta("usuario.prueba");

        Check(!string.IsNullOrWhiteSpace(alta.Secret), "el alta genera un secreto");
        Check(alta.QrPngBase64.Length > 100, "el alta genera un PNG de QR");
        // Firma PNG: los primeros bytes deben ser \x89PNG.
        byte[] png = Convert.FromBase64String(alta.QrPngBase64);
        Check(png is [0x89, 0x50, 0x4E, 0x47, ..], "el QR es un PNG valido");

        // El codigo vigente generado con ese secreto debe validar contra el mismo secreto.
        string codigo = new Totp(Base32Encoding.ToBytes(alta.Secret)).ComputeTotp();
        Check(svc.Verificar(alta.Secret, codigo), "un codigo recien generado es valido");
        Check(svc.Verificar(alta.Secret, $" {codigo} "), "se aceptan espacios alrededor del codigo");
        Check(!svc.Verificar(alta.Secret, "000000") || codigo == "000000", "un codigo arbitrario no valida");

        // Un codigo valido para OTRO secreto no debe abrir esta cuenta.
        var otra = svc.GenerarAlta("otro.usuario");
        string codigoAjeno = new Totp(Base32Encoding.ToBytes(otra.Secret)).ComputeTotp();
        Check(otra.Secret != alta.Secret, "cada alta genera un secreto distinto");
        Check(!svc.Verificar(alta.Secret, codigoAjeno) || codigoAjeno == codigo,
              "el codigo de otro secreto no valida");

        Check(!svc.Verificar(null, codigo), "sin secreto no valida");
        Check(!svc.Verificar(alta.Secret, null), "sin codigo no valida");
        Check(!svc.Verificar(alta.Secret, ""), "codigo vacio no valida");
        Check(!svc.Verificar("no-es-base32-valido!!", codigo), "un secreto corrupto no lanza, solo rechaza");

        Console.WriteLine(ok ? "OK: TOTP paso todos los checks." : "TOTP: uno o mas checks fallaron.");
        return ok;
    }
}
