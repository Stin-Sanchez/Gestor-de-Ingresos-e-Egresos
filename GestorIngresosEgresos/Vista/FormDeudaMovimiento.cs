using GestorIngresosEgresos.Modelo;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace GestorIngresosEgresos.Vista
{
    public partial class FormDeudaMovimiento : Form
    {
        private static readonly Color C_SURFACE = Color.White;
        private static readonly Color C_BG      = Color.FromArgb(247, 248, 252);
        private static readonly Color C_TEXT     = Color.FromArgb(17, 24, 39);
        private static readonly Color C_MUTED    = Color.FromArgb(107, 114, 128);
        private static readonly Color C_BORDER   = Color.FromArgb(209, 213, 219);
        private static readonly Color C_ACCENT   = Color.FromArgb(37, 99, 235);
        private static readonly Color C_HEADER   = Color.FromArgb(30, 41, 59);

        public enum Modo { Nueva, Movimiento }

        private readonly Modo modo;
        private readonly string personaDeuda;

        // Resultados expuestos al llamador
        public string Persona { get; private set; }
        public DireccionDeuda Direccion { get; private set; }
        public decimal Monto { get; private set; }
        public bool EsAbono { get; private set; }
        public string Nota { get; private set; }
        public string Descripcion { get; private set; }

        private TextBox txtPersona;
        private ComboBox cboDireccion;
        private NumericUpDown numMonto;
        private ComboBox cboTipoMov;
        private TextBox txtNota;
        private TextBox txtDescripcion;

        public FormDeudaMovimiento(Modo m, string persona = null)
        {
            modo = m;
            personaDeuda = persona;
            InitializeComponent();
            ConstruirUI();
        }

        private void ConstruirUI()
        {
            bool esNueva = modo == Modo.Nueva;
            this.Text = esNueva ? "Nueva deuda" : $"Movimiento — {personaDeuda}";
            this.Size = new Size(460, esNueva ? 440 : 360);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = C_BG;
            this.Font = new Font("Segoe UI", 9.5F);

            Panel header = new Panel { Height = 52, Dock = DockStyle.Top, BackColor = C_HEADER };
            Label lblTitulo = new Label
            {
                Text = esNueva ? "Nueva deuda" : "Agregar movimiento",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(18, 13)
            };
            header.Controls.Add(lblTitulo);

            Panel contenido = new Panel { Dock = DockStyle.Fill, BackColor = C_SURFACE, Padding = new Padding(26, 18, 26, 10) };

            int y = 14;

            if (esNueva)
            {
                contenido.Controls.Add(CrearLabel("Persona", new Point(26, y)));
                txtPersona = new TextBox
                {
                    Location = new Point(26, y + 20),
                    Width = 390,
                    Font = new Font("Segoe UI", 10F),
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = C_SURFACE,
                    ForeColor = C_TEXT
                };
                contenido.Controls.Add(txtPersona);
                y += 64;

                contenido.Controls.Add(CrearLabel("Direccion", new Point(26, y)));
                cboDireccion = new ComboBox
                {
                    Location = new Point(26, y + 20),
                    Width = 390,
                    Font = new Font("Segoe UI", 10F),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = C_SURFACE
                };
                cboDireccion.Items.AddRange(new object[] { "Me deben (alguien me debe a mi)", "Debo (yo le debo a alguien)" });
                cboDireccion.SelectedIndex = 0;
                contenido.Controls.Add(cboDireccion);
                y += 64;

                contenido.Controls.Add(CrearLabel("Descripcion (opcional)", new Point(26, y)));
                txtDescripcion = new TextBox
                {
                    Location = new Point(26, y + 20),
                    Width = 390,
                    Font = new Font("Segoe UI", 10F),
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = C_SURFACE
                };
                contenido.Controls.Add(txtDescripcion);
                y += 64;
            }
            else
            {
                contenido.Controls.Add(CrearLabel("Tipo de movimiento", new Point(26, y)));
                cboTipoMov = new ComboBox
                {
                    Location = new Point(26, y + 20),
                    Width = 390,
                    Font = new Font("Segoe UI", 10F),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = C_SURFACE
                };
                cboTipoMov.Items.AddRange(new object[] { "Incrementar (prestaron/deben mas)", "Abonar (pagaron/pague algo)" });
                cboTipoMov.SelectedIndex = 0;
                contenido.Controls.Add(cboTipoMov);
                y += 64;

                contenido.Controls.Add(CrearLabel("Nota (opcional)", new Point(26, y)));
                txtNota = new TextBox
                {
                    Location = new Point(26, y + 20),
                    Width = 390,
                    Font = new Font("Segoe UI", 10F),
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = C_SURFACE
                };
                contenido.Controls.Add(txtNota);
                y += 64;
            }

            contenido.Controls.Add(CrearLabel("Monto", new Point(26, y)));
            numMonto = new NumericUpDown
            {
                Location = new Point(26, y + 20),
                Width = 390,
                Font = new Font("Segoe UI", 10F),
                DecimalPlaces = 2,
                Maximum = 999999999,
                Minimum = 0,
                ThousandsSeparator = true,
                BackColor = C_SURFACE
            };
            contenido.Controls.Add(numMonto);

            Panel panelBtns = new Panel { Height = 54, Dock = DockStyle.Bottom, BackColor = C_BG };

            Button btnCancelar = new Button
            {
                Text = "Cancelar",
                Size = new Size(110, 36),
                Location = new Point(26, 9),
                Font = new Font("Segoe UI", 10F),
                BackColor = C_SURFACE,
                ForeColor = C_MUTED,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderColor = C_BORDER;
            btnCancelar.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            Button btnOk = new Button
            {
                Text = esNueva ? "Crear deuda" : "Guardar",
                Size = new Size(130, 36),
                Location = new Point(286, 9),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = C_ACCENT,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += BtnOk_Click;

            panelBtns.Controls.Add(btnCancelar);
            panelBtns.Controls.Add(btnOk);

            this.Controls.Add(contenido);
            this.Controls.Add(panelBtns);
            this.Controls.Add(header);
        }

        private Label CrearLabel(string texto, Point pos) =>
            new Label { Text = texto, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = C_MUTED, AutoSize = true, Location = pos };

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (numMonto.Value <= 0)
            {
                MessageBox.Show("El monto debe ser mayor a cero.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (modo == Modo.Nueva)
            {
                if (string.IsNullOrWhiteSpace(txtPersona.Text))
                {
                    MessageBox.Show("Ingresa el nombre de la persona.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Persona = txtPersona.Text.Trim();
                Direccion = cboDireccion.SelectedIndex == 0 ? DireccionDeuda.ME_DEBEN : DireccionDeuda.DEBO;
                Descripcion = txtDescripcion.Text.Trim();
            }
            else
            {
                EsAbono = cboTipoMov.SelectedIndex == 1;
                Nota = txtNota.Text.Trim();
            }

            Monto = numMonto.Value;
            this.DialogResult = DialogResult.OK;
        }
    }
}
