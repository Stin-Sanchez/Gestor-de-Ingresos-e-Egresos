using GestorIngresosEgresos.Modelo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GestorIngresosEgresos.Vista
{
    public partial class FormMovimiento : Form
    {
        public Movimiento MovimientoEditado { get; private set; }

        private DateTimePicker dtpFecha;
        private TextBox txtNombre;
        private NumericUpDown numMonto;
        private ComboBox cboTipo;
        private TextBox txtDescripcion;
        private Button btnGuardar;
        private Button btnCancelar;
        private Panel panelHeader;
        private Label lblTitulo;

        public FormMovimiento(Movimiento movimiento)
        {
            MovimientoEditado = movimiento ?? new Movimiento { Fecha = DateTime.Now };
            InitializeComponent();
            initComponent();
            CargarDatos();
        }

        private void initComponent()
        {
            this.Text = MovimientoEditado.Id == 0 ? "Nuevo Movimiento" : "Editar Movimiento";
            this.Size = new Size(500, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9F);

            // Panel Header
            panelHeader = new Panel
            {
                Height = 80,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(41, 128, 185)
            };

            lblTitulo = new Label
            {
                Text = MovimientoEditado.Id == 0 ? "➕ NUEVO MOVIMIENTO" : "✏️ EDITAR MOVIMIENTO",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 25)
            };

            panelHeader.Controls.Add(lblTitulo);

            // Panel contenedor de controles
            Panel panelContenido = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30, 20, 30, 20),
                BackColor = Color.White
            };

            int y = 10;

            // Fecha
            Label lblFecha = new Label
            {
                Text = "📅 Fecha:",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Location = new Point(50, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(52, 73, 94)
            };

            dtpFecha = new DateTimePicker
            {
                Location = new Point(50, y + 28),
                Width = 470,
                Font = new Font("Segoe UI", 11F),
                Format = DateTimePickerFormat.Short
            };

            panelContenido.Controls.Add(lblFecha);
            panelContenido.Controls.Add(dtpFecha);
            y += 75;

            // Nombre
            Label lblNombre = new Label
            {
                Text = "📝 Nombre del movimiento:",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Location = new Point(50, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(52, 73, 94)
            };

            txtNombre = new TextBox
            {
                Location = new Point(50, y + 28),
                Width = 470,
                Font = new Font("Segoe UI", 11F),
                BorderStyle = BorderStyle.FixedSingle
            };

            panelContenido.Controls.Add(lblNombre);
            panelContenido.Controls.Add(txtNombre);
            y += 75;

            // Tipo y Monto en la misma línea
            Label lblTipo = new Label
            {
                Text = "🔖 Tipo:",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Location = new Point(50, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(52, 73, 94)
            };

            cboTipo = new ComboBox
            {
                Location = new Point(50, y + 28),
                Width = 220,
                Font = new Font("Segoe UI", 11F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            cboTipo.Items.AddRange(new object[] { "Ingreso", "Egreso" });

            Label lblMonto = new Label
            {
                Text = "💵 Monto:",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Location = new Point(300, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(52, 73, 94)
            };

            numMonto = new NumericUpDown
            {
                Location = new Point(300, y + 28),
                Width = 220,
                Font = new Font("Segoe UI", 11F),
                DecimalPlaces = 2,
                Maximum = 999999999,
                Minimum = 0,
                ThousandsSeparator = true
            };

            panelContenido.Controls.Add(lblTipo);
            panelContenido.Controls.Add(cboTipo);
            panelContenido.Controls.Add(lblMonto);
            panelContenido.Controls.Add(numMonto);
            y += 75;

            // Descripción
            Label lblDesc = new Label
            {
                Text = "📄 Descripción (opcional):",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Location = new Point(50, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(52, 73, 94)
            };

            txtDescripcion = new TextBox
            {
                Location = new Point(50, y + 28),
                Width = 470,
                Height = 90,
                Font = new Font("Segoe UI", 10F),
                Multiline = true,
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.Vertical
            };

            panelContenido.Controls.Add(lblDesc);
            panelContenido.Controls.Add(txtDescripcion);
            y += 130;

            // Panel de botones
            Panel panelBotones = new Panel
            {
                Height = 60,
                Dock = DockStyle.Bottom,
                BackColor = Color.White,
                Padding = new Padding(30, 10, 30, 10)
            };

            btnCancelar = new Button
            {
                Text = "✕ Cancelar",
                Size = new Size(150, 40),
                Location = new Point(60, 10),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            btnGuardar = new Button
            {
                Text = "✓ Guardar",
                Size = new Size(150, 40),
                Location = new Point(375, 10),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            panelBotones.Controls.Add(btnCancelar);
            panelBotones.Controls.Add(btnGuardar);

            this.Controls.Add(panelContenido);
            this.Controls.Add(panelBotones);
            this.Controls.Add(panelHeader);
        }

        private void CargarDatos()
        {
            dtpFecha.Value = MovimientoEditado.Fecha;
            txtNombre.Text = MovimientoEditado.Nombre;
            numMonto.Value = Math.Abs(MovimientoEditado.Monto);

            if (MovimientoEditado.Id == 0)
            {
                cboTipo.SelectedIndex = 0; // Por defecto Ingreso
            }
            else
            {
                cboTipo.SelectedItem = MovimientoEditado.Tipo == TipoMovimiento.INGRESO ? "Ingreso" : "Egreso";
            }

            txtDescripcion.Text = MovimientoEditado.Descripcion;
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("❌ Debe ingresar un nombre para el movimiento.",
                    "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNombre.Focus();
                return;
            }

            if (cboTipo.SelectedIndex < 0)
            {
                MessageBox.Show("❌ Debe seleccionar el tipo de movimiento.",
                    "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboTipo.Focus();
                return;
            }

            if (numMonto.Value <= 0)
            {
                MessageBox.Show("❌ El monto debe ser mayor a cero.",
                    "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                numMonto.Focus();
                return;
            }

            // Asignar valores al movimiento
            MovimientoEditado.Fecha = dtpFecha.Value;
            MovimientoEditado.Nombre = txtNombre.Text.Trim();

            string tipoSeleccionado = cboTipo.SelectedItem.ToString();
            MovimientoEditado.Tipo = tipoSeleccionado == "Ingreso" ? TipoMovimiento.INGRESO : TipoMovimiento.EGRESO;

            // El monto se guarda con signo según el tipo
            MovimientoEditado.Monto = MovimientoEditado.Tipo == TipoMovimiento.INGRESO
                ? numMonto.Value
                : -numMonto.Value;

            MovimientoEditado.Descripcion = txtDescripcion.Text.Trim();

            this.DialogResult = DialogResult.OK;
        }


        private void FormMovimiento_Load(object sender, EventArgs e)
        {

        }
    }
}
    

