using GestorIngresosEgresos.Modelo;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace GestorIngresosEgresos.Vista
{
    public partial class FormNuevaDeuda : Form
    {
        static readonly Color C_SURFACE = Color.White;
        static readonly Color C_MUTED   = Color.FromArgb(100, 116, 139);
        static readonly Color C_DEUDA   = Color.FromArgb(245, 158, 11);

        private TextBox        _txtNombre, _txtAcreedor, _txtDescripcion;
        private NumericUpDown  _nudMonto;
        private DateTimePicker _dtpVencimiento;
        private CheckBox       _chkVencimiento;

        public Deuda Resultado { get; private set; }

        public FormNuevaDeuda()
        {
            InitializeComponent();
            ConstruirUI();
        }

        private void ConstruirUI()
        {
            this.Text            = "Nueva Deuda";
            this.ClientSize      = new Size(440, 340);
            this.BackColor       = C_SURFACE;
            this.Font            = new Font("Segoe UI", 10F);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;

            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6,
                Padding = new Padding(24, 20, 24, 16), BackColor = C_SURFACE
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 5; i++) tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _txtNombre      = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            _txtAcreedor    = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            _nudMonto       = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0.01m, Maximum = 9999999m, DecimalPlaces = 2, ThousandsSeparator = true };
            _txtDescripcion = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };

            var panelVenc = new Panel { Dock = DockStyle.Fill };
            _chkVencimiento = new CheckBox { Text = "Con vencimiento", Location = new Point(0, 10), AutoSize = true, ForeColor = C_MUTED };
            _dtpVencimiento = new DateTimePicker { Location = new Point(136, 6), Width = 160, Format = DateTimePickerFormat.Short, Enabled = false };
            _chkVencimiento.CheckedChanged += (s, e) => _dtpVencimiento.Enabled = _chkVencimiento.Checked;
            panelVenc.Controls.AddRange(new Control[] { _chkVencimiento, _dtpVencimiento });

            tlp.Controls.Add(Lbl("Nombre:"),      0, 0); tlp.Controls.Add(_txtNombre,      1, 0);
            tlp.Controls.Add(Lbl("Acreedor:"),    0, 1); tlp.Controls.Add(_txtAcreedor,    1, 1);
            tlp.Controls.Add(Lbl("Monto ($):"),   0, 2); tlp.Controls.Add(_nudMonto,       1, 2);
            tlp.Controls.Add(Lbl("Descripcion:"), 0, 3); tlp.Controls.Add(_txtDescripcion, 1, 3);
            tlp.Controls.Add(Lbl("Vencimiento:"), 0, 4); tlp.Controls.Add(panelVenc,       1, 4);

            var flpBtn = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = C_SURFACE };
            var btnCancelar = new Button { Text = "Cancelar", Size = new Size(90, 32), Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat };
            var btnGuardar  = new Button
            {
                Text = "Guardar", Size = new Size(90, 32), BackColor = C_DEUDA, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            btnGuardar.Click  += BtnGuardar_Click;
            flpBtn.Controls.Add(btnGuardar);
            flpBtn.Controls.Add(btnCancelar);
            tlp.Controls.Add(flpBtn, 0, 5);
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
            if (string.IsNullOrWhiteSpace(_txtNombre.Text))
            { MessageBox.Show("El nombre es obligatorio.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(_txtAcreedor.Text))
            { MessageBox.Show("El acreedor es obligatorio.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (_nudMonto.Value <= 0)
            { MessageBox.Show("El monto debe ser mayor a cero.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            Resultado = new Deuda
            {
                Nombre           = _txtNombre.Text.Trim(),
                Acreedor         = _txtAcreedor.Text.Trim(),
                MontoOriginal    = _nudMonto.Value,
                FechaInicio      = DateTime.Today,
                FechaVencimiento = _chkVencimiento.Checked ? (DateTime?)_dtpVencimiento.Value.Date : null,
                Descripcion      = _txtDescripcion.Text.Trim()
            };
            DialogResult = DialogResult.OK;
        }
    }
}
