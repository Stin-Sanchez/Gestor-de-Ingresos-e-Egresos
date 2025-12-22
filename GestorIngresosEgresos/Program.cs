using GestorIngresosEgresos.Vista;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GestorIngresosEgresos
{
    static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // AQUÍ LLAMAS A TU FORMULARIO
            // Cambia 'Form1' por el nombre de tu clase Form
            Application.Run(new Form2());
        }
    }
}
