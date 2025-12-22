using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestorIngresosEgresos.Util
{
    public class ConexionDB
    {
        //Instancia estática (el Singleton)
        private static ConexionDB instancia;

        //Objeto que guarda la conexión real a MySQL
        private MySqlConnection connection;

        //Constructor PRIVADO
        private ConexionDB()
        {
            try
            {
                Console.WriteLine("--- Intentando conectar al servidor local ---");

             
                string servidor = "TU SERVER";
                string baseDatos = "TU DB"; 
                string usuario = "TU USER";
                string password = "TU PASSWORD"; 

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

        //  Método estático para obtener la instancia (Singleton)
        public static ConexionDB GetInstance()
        {
            if (instancia == null)
            {
                instancia = new ConexionDB();
            }
            return instancia;
        }

        //  Método auxiliar para  usar la conexión en las consultas
        public MySqlConnection GetConnection()
        {
            return connection;
        }
    }

}

