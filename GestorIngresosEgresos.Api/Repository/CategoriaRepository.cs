using GestorIngresosEgresos.Api.Data;
using GestorIngresosEgresos.Api.Modelo;
using MySql.Data.MySqlClient;

namespace GestorIngresosEgresos.Api.Repository;

// Catalogo global de solo lectura, no es por usuario.
public class CategoriaRepository(Db db)
{
    public List<CategoriaGasto> ObtenerTodas()
    {
        var lista = new List<CategoriaGasto>();
        using var conn = db.Open();
        using var cmd = new MySqlCommand("SELECT * FROM categorias_gasto ORDER BY nombre", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            lista.Add(new CategoriaGasto { Id = r.GetInt32("id"), Nombre = r.GetString("nombre") });
        return lista;
    }
}
