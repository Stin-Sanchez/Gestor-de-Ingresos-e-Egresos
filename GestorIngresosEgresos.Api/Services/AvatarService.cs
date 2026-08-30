namespace GestorIngresosEgresos.Api.Services;

// Guarda los avatares como archivos en un directorio montado (volumen Docker);
// la base solo conserva el nombre del archivo.
public class AvatarService
{
    private const long MaxBytes = 2 * 1024 * 1024;

    // El content-type y el nombre los manda el cliente, asi que no se usan para decidir
    // nada: el tipo real se deduce de los magic bytes y el nombre lo genera el servidor.
    private static readonly (string Ext, byte[] Firma)[] Formatos =
    [
        (".png",  [0x89, 0x50, 0x4E, 0x47]),
        (".jpg",  [0xFF, 0xD8, 0xFF]),
        (".gif",  [0x47, 0x49, 0x46, 0x38]),
        (".webp", [0x52, 0x49, 0x46, 0x46]), // "RIFF"; el marcador WEBP se valida aparte
    ];

    private readonly string _directorio;

    public AvatarService(IConfiguration config)
    {
        _directorio = config["Avatares:Directorio"] ?? Path.Combine(AppContext.BaseDirectory, "data", "avatares");
        Directory.CreateDirectory(_directorio);
    }

    public string Ruta(string archivo) => Path.Combine(_directorio, archivo);

    public bool Existe(string archivo) => File.Exists(Ruta(archivo));

    public static string TipoMime(string archivo) => Path.GetExtension(archivo) switch
    {
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        _ => "image/jpeg",
    };

    public async Task<string> GuardarAsync(int usuarioId, IFormFile archivo)
    {
        if (archivo.Length == 0) throw new ArgumentException("El archivo esta vacio.");
        if (archivo.Length > MaxBytes) throw new ArgumentException("La imagen no puede pesar mas de 2 MB.");

        using var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria);
        byte[] datos = memoria.ToArray();

        string ext = DetectarExtension(datos)
            ?? throw new ArgumentException("El archivo no es una imagen valida (PNG, JPG, GIF o WEBP).");

        string nombre = $"{usuarioId}-{Guid.NewGuid():N}{ext}";
        await File.WriteAllBytesAsync(Ruta(nombre), datos);
        return nombre;
    }

    public void Eliminar(string? archivo)
    {
        if (string.IsNullOrWhiteSpace(archivo)) return;
        var ruta = Ruta(archivo);
        if (File.Exists(ruta)) File.Delete(ruta);
    }

    private static string? DetectarExtension(byte[] datos)
    {
        foreach (var (ext, firma) in Formatos)
        {
            if (datos.Length < firma.Length) continue;
            if (!datos.Take(firma.Length).SequenceEqual(firma)) continue;
            // Un RIFF puede ser WAV o AVI; solo es imagen si trae el marcador WEBP.
            if (ext == ".webp" && !(datos.Length >= 12 && datos[8] == 0x57 && datos[9] == 0x45 && datos[10] == 0x42 && datos[11] == 0x50))
                continue;
            return ext;
        }
        return null;
    }
}
