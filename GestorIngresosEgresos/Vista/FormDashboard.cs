using GestorIngresosEgresos.Util;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace GestorIngresosEgresos.Vista
{
    public partial class FormDashboard : Form
    {
        static readonly Color C_SIDEBAR = Color.FromArgb(30, 41, 59);
        static readonly Color C_ACCENT  = Color.FromArgb(37, 99, 235);
        static readonly Color C_BG      = Color.FromArgb(248, 250, 252);
        static readonly Color C_MUTED   = Color.FromArgb(148, 163, 184);
        static readonly Color C_LOGO_BG = Color.FromArgb(15, 23, 42);

        private Panel  _panelContenedor;
        private Button _btnActivo;

        public FormDashboard()
        {
            InitializeComponent();
            ConstruirUI();
            if (Program.AppIcon != null) this.Icon = Program.AppIcon;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize      = new Size(1400, 850);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.Text            = "Gestor Financiero";
            this.BackColor       = C_BG;
            this.Font            = new Font("Segoe UI", 10F);
            this.Name            = "FormDashboard";
            this.ResumeLayout(false);
        }

        private void ConstruirUI()
        {
            var sidebar = new Panel { Dock = DockStyle.Left, Width = 220, BackColor = C_SIDEBAR };

            var panelLogo = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = C_LOGO_BG };
            panelLogo.Controls.Add(new Label
            {
                Text      = "Gestor Financiero",
                Font      = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            });

            var btnIngresos     = CrearNavBtn("Ingresos y Egresos");
            var btnPresupuestos = CrearNavBtn("Presupuestos");
            var btnDeudas       = CrearNavBtn("Deudas");

            btnIngresos.Click     += (s, e) => { AbrirForm(new FormPeriodo()); MarcarActivo(btnIngresos); };
            btnPresupuestos.Click += (s, e) => { AbrirForm(new FormPresupuestos()); MarcarActivo(btnPresupuestos); };
            btnDeudas.Click       += (s, e) => { AbrirForm(new FormDeudas()); MarcarActivo(btnDeudas); };

            var panelNav = new Panel { Dock = DockStyle.Fill, BackColor = C_SIDEBAR };
            panelNav.Controls.Add(btnDeudas);
            panelNav.Controls.Add(btnPresupuestos);
            panelNav.Controls.Add(btnIngresos);

            var btnSalir = new Button
            {
                Text      = "Cerrar sesion",
                Dock      = DockStyle.Bottom,
                Height    = 45,
                Font      = new Font("Segoe UI", 10F),
                ForeColor = C_MUTED,
                BackColor = C_SIDEBAR,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnSalir.FlatAppearance.BorderSize = 0;
            btnSalir.Click += (s, e) => CerrarSesion();

            sidebar.Controls.Add(panelNav);
            sidebar.Controls.Add(btnSalir);
            sidebar.Controls.Add(panelLogo);

            _panelContenedor = new Panel { Dock = DockStyle.Fill, BackColor = C_BG };

            this.Controls.Add(_panelContenedor);
            this.Controls.Add(sidebar);

            AbrirForm(new FormPeriodo());
            MarcarActivo(btnIngresos);
        }

        private Button CrearNavBtn(string texto)
        {
            var btn = new Button
            {
                Text      = texto,
                Dock      = DockStyle.Top,
                Height    = 50,
                Font      = new Font("Segoe UI", 11F),
                ForeColor = C_MUTED,
                BackColor = C_SIDEBAR,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(22, 0, 0, 0),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize        = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(51, 65, 85);
            return btn;
        }

        private void MarcarActivo(Button btn)
        {
            if (_btnActivo != null)
            {
                _btnActivo.BackColor = C_SIDEBAR;
                _btnActivo.ForeColor = C_MUTED;
            }
            btn.BackColor = C_ACCENT;
            btn.ForeColor = Color.White;
            _btnActivo = btn;
        }

        private void AbrirForm(Form hijo)
        {
            foreach (Control c in _panelContenedor.Controls)
                if (c is Form f) { f.Close(); f.Dispose(); }

            _panelContenedor.Controls.Clear();
            hijo.TopLevel        = false;
            hijo.FormBorderStyle = FormBorderStyle.None;
            hijo.Dock            = DockStyle.Fill;
            _panelContenedor.Controls.Add(hijo);
            hijo.Show();
        }

        private void CerrarSesion()
        {
            if (MessageBox.Show("Deseas cerrar la sesion?", "Cerrar sesion",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Sesion.UsuarioId = 0;
                Sesion.Username  = null;
                Application.Exit();
            }
        }
    }
}
