using GestorIngresosEgresos.Vista;
using System;
using System.Windows.Forms;

namespace GestorIngresosEgresos
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (FormLogin login = new FormLogin())
            {
                if (login.ShowDialog() == DialogResult.OK)
                {
                    Application.Run(new FormDashboard());
                }
            }
        }
    }

}
