using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Util;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestorIngresosEgresos.Repository
{
    public class RepositoryImp : Repository<Movimiento>
    {

        // Variable para usar la conexión del Singleton
        private MySqlConnection connection;

        public RepositoryImp()
        {
            //Obtenemos la conexion singleton que manejara todas las transacciones
            connection = DBConnection.GetInstance().GetConnection();
        }

        public Movimiento actualizarMovimiento(Movimiento t)
        {
            string query = @"UPDATE movimientos 
                           SET fecha = @fecha, 
                               nombre = @nombre, 
                               monto = @monto, 
                               tipo = @tipo, 
                               descripcion = @descripcion 
                           WHERE id = @id";

            try
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@id", t.Id);
                cmd.Parameters.AddWithValue("@fecha", t.Fecha);
                cmd.Parameters.AddWithValue("@nombre", t.Nombre);
                cmd.Parameters.AddWithValue("@monto", t.Monto);
                cmd.Parameters.AddWithValue("@tipo", t.Tipo.ToString()); // Convertir enum a string
                cmd.Parameters.AddWithValue("@descripcion", t.Descripcion ?? "");

                int filasAfectadas = cmd.ExecuteNonQuery();

                if (filasAfectadas == 0)
                {
                    throw new Exception($"No se encontró el movimiento con ID {t.Id}");
                }

                return t;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error al actualizar: " + e.Message);
                throw;
            }
        }

        public void eliminarMovimiento(int id)
        {
            string query = "DELETE FROM movimientos WHERE id = @id";

            try
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@id", id);

                int filasAfectadas = cmd.ExecuteNonQuery();

                if (filasAfectadas == 0)
                {
                    throw new Exception($"No se encontró el movimiento con ID {id}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error al eliminar: " + e.Message);
                throw;
            }
        }

        public Movimiento guardarMovimiento(Movimiento t)
        {
            string query = @"INSERT INTO movimientos (fecha, nombre, monto, tipo, descripcion) 
                           VALUES (@fecha, @nombre, @monto, @tipo, @descripcion);
                           SELECT LAST_INSERT_ID();";

            try
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@fecha", t.Fecha);
                cmd.Parameters.AddWithValue("@nombre", t.Nombre);
                cmd.Parameters.AddWithValue("@monto", t.Monto);
                cmd.Parameters.AddWithValue("@tipo", t.Tipo.ToString()); // Convertir enum a string
                cmd.Parameters.AddWithValue("@descripcion", t.Descripcion ?? "");

                // Ejecutar y obtener el ID generado
                object result = cmd.ExecuteScalar();
                if (result != null)
                {
                    t.Id = Convert.ToInt32(result);
                }

                return t;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error al guardar: " + e.Message);
                throw;
            }

        }

        public List<Movimiento> obtenerMovimientos()
        {
            List<Movimiento> listaMovimientos = new List<Movimiento>();
            string query = "SELECT *FROM movimientos ORDER BY fecha DESC";
            try
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Movimiento mov = new Movimiento();
                        mov.Id = reader.GetInt32("id");
                        mov.Nombre = reader.GetString("nombre");
                        mov.Monto = reader.GetDecimal("monto");
                        //Recogemos con el reader desde la db como string
                        string tipoMov = reader.GetString("tipo");
                        //Luego parseamos al tipo de clase Enum
                        mov.Tipo = (TipoMovimiento)Enum.Parse(typeof(TipoMovimiento), tipoMov);
                        mov.Descripcion = reader.GetString("descripcion");
                        mov.Fecha = reader.GetDateTime("fecha");
                        listaMovimientos.Add(mov);

                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Error al listar: " + e.Message);
                throw;
            }

            return listaMovimientos;


        }

        public Movimiento obtenerPorId(int id)
        {
            string query = "SELECT * FROM movimientos WHERE id = @id";
            Movimiento mov = null;

            try
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@id", id);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        mov = new Movimiento
                        {
                            Id = reader.GetInt32("id"),
                            Nombre = reader.GetString("nombre"),
                            Monto = reader.GetDecimal("monto"),
                            Descripcion = reader.GetString("descripcion"),
                            Fecha = reader.GetDateTime("fecha")
                        };

                        // Convertir string a enum
                        string tipoString = reader.GetString("tipo");
                        if (Enum.TryParse<TipoMovimiento>(tipoString, out TipoMovimiento tipoEnum))
                        {
                            mov.Tipo = tipoEnum;
                        }
                    }
                    return mov;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error al obtener por ID: " + e.Message);
                throw;
            }
          
        }
    
    }
    }


