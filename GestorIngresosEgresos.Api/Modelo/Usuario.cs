namespace GestorIngresosEgresos.Api.Modelo;

public class Usuario
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}
