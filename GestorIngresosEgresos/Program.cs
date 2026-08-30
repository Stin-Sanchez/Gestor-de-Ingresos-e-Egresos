using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Vista;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace GestorIngresosEgresos
{
    static class Program
    {
        // Embebido en el assembly via <EmbeddedResource> en .csproj
        public static Icon AppIcon { get; private set; }

        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--selftest")
                return PresupuestoResumen.SelfCheck() ? 0 : 1;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                string icoPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
                AppIcon = new Icon(icoPath);
            }
            catch { }

            using (FormLogin login = new FormLogin())
            {
                if (login.ShowDialog() == DialogResult.OK)
                    Application.Run(new FormDashboard());
            }
            return 0;
        }
    }
}
