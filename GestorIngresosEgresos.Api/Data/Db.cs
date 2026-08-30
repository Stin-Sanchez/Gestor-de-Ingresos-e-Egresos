using MySql.Data.MySqlClient;

namespace GestorIngresosEgresos.Api.Data;

// Reemplaza al singleton ConexionDB de la app de escritorio: en un servidor web,
// varias requests concurrentes necesitan su propia conexion, no una compartida.
public class Db(IConfiguration config)
{
    private readonly string _connectionString =
        config.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:Default en la configuracion.");

    public MySqlConnection Open()
    {
        var conn = new MySqlConnection(_connectionString);
        conn.Open();
        return conn;
    }
}
