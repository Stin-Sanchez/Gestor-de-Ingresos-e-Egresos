using GestorIngresosEgresos.Modelo;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace GestorIngresosEgresos.Vista
{
    // Registra un consumo contra un sobre: "hoy me gaste 3.75 en almuerzo".
    public partial class FormConsumoDialog : Form
    {
        static readonly Color C_SURFACE = Color.White;
        static readonly Color C_MUTED   = Color.FromArgb(100, 116, 139);
        static readonly Color C_GASTO   = Color.FromArgb(239, 68, 68);

        private readonly int     _gastoId;
        private readonly int     _existingId;
        private readonly decimal _disponible;
        private readonly string  _tituloSobre;

        private DateTimePicker _dtpFecha;
        private TextBox        _txtDescripcion;
        private NumericUpDown  _nudMonto;

        public Consumo Resultado { get; private set; }

        public FormConsumoDialog(int gastoId, string tituloSobre, decimal disponible, Consumo existing = null)
        {
            _gastoId     = gastoId;
            _tituloSobre = tituloSobre;
            _disponible  = disponible;
            _existingId  = existing?.Id ?? 0;
            InitializeComponent();
            ConstruirUI();

            if (existing != null)
            {
                this.Text            = "Editar consumo";
                _dtpFecha.Value      = existing.Fecha;
                _txtDescripcion.Text = existing.Descripcion;
                _nudMonto.Value      = Math.Max(_nudMonto.Minimum, Math.Min(_nudMonto.Maximum, existing.Monto));
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "FormConsumoDialog";
            this.ResumeLayout(false);
        }

        private void ConstruirUI()
        {
            this.Text            = "Nuevo consumo";
            this.ClientSize      = new Size(420, 290);
            this.BackColor       = C_SURFACE;
            this.Font            = new Font("Segoe UI", 10F);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;

            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5,
                Padding = new Padding(24, 20, 24, 16), BackColor = C_SURFACE
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            for (int i = 0; i < 3; i++) tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var lblSobre = new Label
            {
                Text = $"{_tituloSobre}  -  disponible ${_disponible:N2}",
                Dock = DockStyle.Fill, ForeColor = C_MUTED, Font = new Font("Segoe UI", 9.5F),
                TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
            };
            tlp.Controls.Add(lblSobre, 0, 0);
            tlp.SetColumnSpan(lblSobre, 2);

            _dtpFecha       = new DateTimePicker { Dock = DockStyle.Fill, Value = DateTime.Today, Format = DateTimePickerFormat.Short };
            _txtDescripcion = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            _nudMonto       = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0m, Maximum = 9999999m, DecimalPlaces = 2, ThousandsSeparator = true };

            tlp.Controls.Add(Lbl("Fecha:"),     0, 1); tlp.Controls.Add(_dtpFecha,       1, 1);
            tlp.Controls.Add(Lbl("En que:"),    0, 2); tlp.Controls.Add(_txtDescripcion, 1, 2);
            tlp.Controls.Add(Lbl("Monto ($):"), 0, 3); tlp.Controls.Add(_nudMonto,       1, 3);

            var flpBtn = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = C_SURFACE };
            var btnCancelar = new Button { Text = "Cancelar", Size = new Size(90, 32), Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat };
            var btnGuardar  = new Button
            {
                Text = "Guardar", Size = new Size(90, 32), BackColor = C_GASTO, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            btnGuardar.Click  += BtnGuardar_Click;
            flpBtn.Controls.Add(btnGuardar);
            flpBtn.Controls.Add(btnCancelar);
            tlp.Controls.Add(flpBtn, 0, 4);
            tlp.SetColumnSpan(flpBtn, 2);

            this.Controls.Add(tlp);
            this.AcceptButton = btnGuardar;
            this.CancelButton = btnCancelar;
        }

        private Label Lbl(string t) => new Label
        {
            Text = t, Dock = DockStyle.Fill, ForeColor = C_MUTED,
            Font = new Font("Segoe UI", 10F), TextAlign = ContentAlignment.MiddleRight
        };

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtDescripcion.Text))
            { MessageBox.Show("Escribe en que gastaste.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (_nudMonto.Value <= 0)
            { MessageBox.Show("El monto debe ser mayor a cero.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            Resultado = new Consumo
            {
                Id          = _existingId,
                GastoId     = _gastoId,
                Fecha       = _dtpFecha.Value.Date,
                Descripcion = _txtDescripcion.Text.Trim(),
                Monto       = _nudMonto.Value
            };
            DialogResult = DialogResult.OK;
        }
    }
}
