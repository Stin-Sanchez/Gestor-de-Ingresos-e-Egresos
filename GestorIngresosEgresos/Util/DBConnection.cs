using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestorIngresosEgresos.Util
{
    public class DBConnection
    {
        //Instancia estática (el Singleton)
        private static DBConnection instancia;

        //Objeto que guarda la conexión real a MySQL
        private MySqlConnection connection;

        //Constructor PRIVADO
        private DBConnection()
        {
            try
            {
                Console.WriteLine("--- Intentando conectar al servidor local ---");

             
                string servidor = "localhost";
                string baseDatos = "gestion_gastos"; 
                string usuario = "root";
                string password = "190904"; 

                string cadenaConexion = $"Server={servidor};Database={baseDatos};Uid={usuario};Pwd={password};";

                connection = new MySqlConnection(cadenaConexion);
                connection.Open();

                // SI LLEGA AQUI, ES QUE CONECTÓ
                Console.WriteLine("\n CONEXIÓN EXITOSA: La base de datos está lista.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine(" ERROR DE CONEXIÓN: " + ex.Message);
            }
        }

        // 4. Método estático para obtener la instancia (Singleton)
        public static DBConnection GetInstance()
        {
            if (instancia == null)
            {
                instancia = new DBConnection();
            }
            return instancia;
        }

        // 5. Método auxiliar para  usar la conexión en las consultas
        public MySqlConnection GetConnection()
        {
            return connection;
        }
    }

}

