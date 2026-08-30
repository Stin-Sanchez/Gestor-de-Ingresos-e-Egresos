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
}
