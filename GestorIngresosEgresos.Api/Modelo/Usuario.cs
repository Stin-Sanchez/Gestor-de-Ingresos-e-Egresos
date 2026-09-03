namespace GestorIngresosEgresos.Api.Modelo;

public class Usuario
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? Email { get; set; }
    public string? Avatar { get; set; }
    public string? TotpSecret { get; set; }
    public bool TotpActivo { get; set; }

    // Configuracion de periodos. Vive aqui y no en una tabla aparte porque son dos
    // valores por usuario; una tabla clave/valor seria flexibilidad que nadie pidio.
    public int DiaCorte { get; set; } = Periodo.DiaCortePorDefecto;
    public int DiasGracia { get; set; } = Periodo.DiasGraciaPorDefecto;
}
