using GestorIngresosEgresos.Controller;
using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Util;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace GestorIngresosEgresos.Vista
{
    public partial class FormLogin : Form
    {
        // Colores profesionales
        private static readonly Color C_SIDEBAR  = Color.FromArgb(30, 41, 59);
        private static readonly Color C_BG       = Color.FromArgb(247, 248, 252);
        private static readonly Color C_SURFACE  = Color.White;
        private static readonly Color C_TEXT      = Color.FromArgb(17, 24, 39);
        private static readonly Color C_MUTED     = Color.FromArgb(107, 114, 128);
        private static readonly Color C_BORDER    = Color.FromArgb(209, 213, 219);
        private static readonly Color C_ACCENT    = Color.FromArgb(37, 99, 235);
        private static readonly Color C_ERROR     = Color.FromArgb(220, 38, 38);

        private int intentosFallidos = 0;
        private Timer lockTimer;
        private int lockSegundos;

        private TextBox txtUsuario;
        private TextBox txtPassword;
        private Button btnMostrarPass;
        private Button btnEntrar;
        private Label lblError;
        private Label lblLock;

        public FormLogin()
        {
            InitializeComponent();
            ConstruirUI();
            if (Program.AppIcon != null) this.Icon = Program.AppIcon;
        }

        private void ConstruirUI()
        {
            this.BackColor = C_SIDEBAR;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10F);

            // ── Panel izquierdo: marca ───────────────────────────────
            var panelLeft = new Panel { Width = 260, Dock = DockStyle.Left, BackColor = C_SIDEBAR };

            panelLeft.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var rect = new Rectangle(94, 122, 72, 72);
                using (var br = new SolidBrush(C_ACCENT))
                    g.FillEllipse(br, rect);
                using (var sf = new System.Drawing.StringFormat { Alignment = System.Drawing.StringAlignment.Center, LineAlignment = System.Drawing.StringAlignment.Center })
                using (var f = new Font("Segoe UI", 26F, FontStyle.Bold))
                    g.DrawString("$", f, Brushes.White, rect, sf);
            };

            var lblAppName = new Label
            {
                Text      = "Gestor\nFinanciero",
                Font      = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize  = false,
                Size      = new Size(260, 68),
                TextAlign = ContentAlignment.MiddleCenter,
                Location  = new Point(0, 208)
            };

            var lblTagline = new Label
            {
                Text      = "Tus finanzas, bajo control",
                Font      = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize  = false,
                Size      = new Size(260, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                Location  = new Point(0, 282)
            };

            var lineaDeco = new Panel
            {
                Size      = new Size(40, 3),
                BackColor = C_ACCENT,
                Location  = new Point(110, 318)
            };

            panelLeft.Controls.AddRange(new Control[] { lblAppName, lblTagline, lineaDeco });

            // ── Panel derecho: formulario ────────────────────────────
            var panelRight = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var lblTitle = new Label
            {
                Text      = "Bienvenido",
                Font      = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = C_TEXT,
                AutoSize  = false,
                Size      = new Size(324, 34),
                TextAlign = ContentAlignment.MiddleLeft,
                Location  = new Point(48, 64)
            };

            var lblSub = new Label
            {
                Text      = "Ingresa tus credenciales para continuar",
                Font      = new Font("Segoe UI", 9.5F),
                ForeColor = C_MUTED,
                AutoSize  = false,
                Size      = new Size(324, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Location  = new Point(48, 102)
            };

            var lblU = FieldLabel("Usuario", new Point(48, 144));
            txtUsuario = FieldInput(new Point(48, 168));
            txtUsuario.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtPassword.Focus(); };
            txtUsuario.Enter   += (s, e) => txtUsuario.BackColor = Color.FromArgb(239, 246, 255);
            txtUsuario.Leave   += (s, e) => txtUsuario.BackColor = Color.White;

            var lblP = FieldLabel("Contraseña", new Point(48, 218));

            var passWrap = new Panel { Location = new Point(48, 242), Size = new Size(324, 38), BackColor = Color.White };
            passWrap.Paint += (s, e) =>
                ControlPaint.DrawBorder(e.Graphics, passWrap.ClientRectangle,
                    C_BORDER, 1, ButtonBorderStyle.Solid, C_BORDER, 1, ButtonBorderStyle.Solid,
                    C_BORDER, 1, ButtonBorderStyle.Solid, C_BORDER, 1, ButtonBorderStyle.Solid);

            txtPassword = new TextBox
            {
                BorderStyle  = BorderStyle.None,
                Font         = new Font("Segoe UI", 10.5F),
                ForeColor    = C_TEXT,
                PasswordChar = '●',
                Location     = new Point(10, 8),
                Size         = new Size(268, 22),
                BackColor    = Color.White
            };
            txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) IntentarLogin(); };
            txtPassword.Enter   += (s, e) => passWrap.BackColor = Color.FromArgb(239, 246, 255);
            txtPassword.Leave   += (s, e) => passWrap.BackColor = Color.White;

            btnMostrarPass = new Button
            {
                Text      = "ver",
                Font      = new Font("Segoe UI", 8.5F),
                ForeColor = C_MUTED,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Location  = new Point(284, 7),
                Size      = new Size(38, 24),
                Cursor    = Cursors.Hand
            };
            btnMostrarPass.FlatAppearance.BorderSize = 0;
            btnMostrarPass.Click += (s, e) =>
            {
                txtPassword.PasswordChar = txtPassword.PasswordChar == '●' ? '\0' : '●';
                btnMostrarPass.Text = txtPassword.PasswordChar == '\0' ? "ocultar" : "ver";
            };
            passWrap.Controls.Add(txtPassword);
            passWrap.Controls.Add(btnMostrarPass);

            lblError = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 9F),
                ForeColor = C_ERROR,
                AutoSize  = false,
                Size      = new Size(324, 18),
                Location  = new Point(48, 288),
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblLock = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 8.5F),
                ForeColor = C_MUTED,
                AutoSize  = false,
                Size      = new Size(324, 16),
                Location  = new Point(48, 306),
                TextAlign = ContentAlignment.MiddleLeft
            };

            btnEntrar = new Button
            {
                Text      = "Entrar",
                Font      = new Font("Segoe UI", 11F, FontStyle.Bold),
                BackColor = C_ACCENT,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(324, 46),
                Location  = new Point(48, 332),
                Cursor    = Cursors.Hand
            };
            btnEntrar.FlatAppearance.BorderSize = 0;
            btnEntrar.Click += (s, e) => IntentarLogin();

            panelRight.Controls.AddRange(new Control[]
            {
                lblTitle, lblSub,
                lblU, txtUsuario,
                lblP, passWrap,
                lblError, lblLock, btnEntrar
            });

            // Fill primero, luego Left (orden de Dock)
            this.Controls.Add(panelRight);
            this.Controls.Add(panelLeft);
            this.AcceptButton = btnEntrar;
        }

        private Label FieldLabel(string text, Point pos) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = C_MUTED,
            AutoSize  = true,
            Location  = pos
        };

        private TextBox FieldInput(Point pos) => new TextBox
        {
            Location    = pos,
            Size        = new Size(324, 38),
            Font        = new Font("Segoe UI", 10.5F),
            ForeColor   = C_TEXT,
            BackColor   = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        private void IntentarLogin()
        {
            if (!btnEntrar.Enabled) return;

            string usuario = txtUsuario.Text.Trim();
            string pass    = txtPassword.Text;

            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(pass))
            {
                MostrarError("Completa todos los campos.");
                return;
            }

            try
            {
                UsuarioController ctrl = new UsuarioController();
                Usuario u = ctrl.Login(usuario, pass);

                if (u != null)
                {
                    Sesion.UsuarioId = u.Id;
                    Sesion.Username  = u.Username;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    intentosFallidos++;
                    txtPassword.Clear();
                    txtPassword.Focus();

                    if (intentosFallidos >= 3)
                    {
                        IniciarBloqueo();
                    }
                    else
                    {
                        MostrarError($"Usuario o contraseña incorrectos. ({intentosFallidos}/3 intentos)");
                    }
                }
            }
            catch (Exception ex)
            {
                MostrarError("Error de conexión: " + ex.Message);
            }
        }

        private void MostrarError(string msg)
        {
            lblError.Text = msg;
        }

        private void IniciarBloqueo()
        {
            lockSegundos = 30;
            btnEntrar.Enabled = false;
            btnEntrar.BackColor = Color.FromArgb(156, 163, 175);
            lblError.Text = "Demasiados intentos fallidos.";

            lockTimer = new Timer { Interval = 1000 };
            lockTimer.Tick += (s, e) =>
            {
                lockSegundos--;
                lblLock.Text = $"Espera {lockSegundos}s para intentarlo de nuevo.";
                if (lockSegundos <= 0)
                {
                    lockTimer.Stop();
                    intentosFallidos = 0;
                    btnEntrar.Enabled = true;
                    btnEntrar.BackColor = Color.FromArgb(37, 99, 235);
                    lblError.Text = "";
                    lblLock.Text  = "";
                    txtUsuario.Focus();
                }
            };
            lockTimer.Start();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            lockTimer?.Stop();
            base.OnFormClosed(e);
        }
    }
}
