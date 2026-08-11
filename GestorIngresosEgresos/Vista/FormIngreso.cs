using GestorIngresosEgresos.Modelo;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace GestorIngresosEgresos.Vista
{
    public partial class FormIngreso : Form
    {
        static readonly Color C_SURFACE = Color.White;
        static readonly Color C_MUTED   = Color.FromArgb(100, 116, 139);
        static readonly Color C_INGRESO = Color.FromArgb(16, 185, 129);

        private readonly int  _periodoId;
        private readonly int? _existingId;

        private DateTimePicker _dtpFecha;
        private ComboBox       _cboTipo;
        private TextBox        _txtDescripcion;
        private NumericUpDown  _nudMonto;

        public Ingreso Resultado { get; private set; }

        public FormIngreso(int periodoId, Ingreso existing = null)
        {
            _periodoId  = periodoId;
            _existingId = existing?.Id;
            InitializeComponent();
            ConstruirUI();
            if (existing != null)
            {
                this.Text            = "Editar Ingreso";
                _dtpFecha.Value      = existing.Fecha;
                _cboTipo.SelectedIndex = _cboTipo.Items.IndexOf(existing.Tipo.ToString());
                _txtDescripcion.Text = existing.Descripcion;
                _nudMonto.Value      = existing.Monto;
            }
        }

        private void ConstruirUI()
        {
            this.Text            = "Nuevo Ingreso";
            this.ClientSize      = new Size(420, 300);
            this.BackColor       = C_SURFACE;
            this.Font            = new Font("Segoe UI", 10F);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;

            var tlp = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 5,
                Padding     = new Padding(24, 20, 24, 16),
                BackColor   = C_SURFACE
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 4; i++) tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _dtpFecha       = new DateTimePicker { Dock = DockStyle.Fill, Value = DateTime.Today, Format = DateTimePickerFormat.Short };
            _cboTipo        = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _txtDescripcion = new TextBox  { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            _nudMonto       = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0.01m, Maximum = 9999999m, DecimalPlaces = 2, ThousandsSeparator = true };
            _cboTipo.Items.AddRange(new object[] { "SUELDO", "EXTRA", "OTRO" });
            _cboTipo.SelectedIndex = 0;

            tlp.Controls.Add(Lbl("Fecha:"),       0, 0); tlp.Controls.Add(_dtpFecha,       1, 0);
            tlp.Controls.Add(Lbl("Tipo:"),        0, 1); tlp.Controls.Add(_cboTipo,        1, 1);
            tlp.Controls.Add(Lbl("Descripcion:"), 0, 2); tlp.Controls.Add(_txtDescripcion, 1, 2);
            tlp.Controls.Add(Lbl("Monto ($):"),   0, 3); tlp.Controls.Add(_nudMonto,       1, 3);

            var flpBtn = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = C_SURFACE };
            var btnCancelar = new Button { Text = "Cancelar", Size = new Size(90, 32), Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat };
            var btnGuardar  = new Button
            {
                Text = "Guardar", Size = new Size(90, 32), BackColor = C_INGRESO, ForeColor = Color.White,
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
            { MessageBox.Show("La descripcion es obligatoria.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (_nudMonto.Value <= 0)
            { MessageBox.Show("El monto debe ser mayor a cero.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            Resultado = new Ingreso
            {
                Id          = _existingId ?? 0,
                PeriodoId   = _periodoId,
                Fecha       = _dtpFecha.Value.Date,
                Tipo        = (TipoIngreso)Enum.Parse(typeof(TipoIngreso), _cboTipo.SelectedItem.ToString()),
                Descripcion = _txtDescripcion.Text.Trim(),
                Monto       = _nudMonto.Value
            };
            DialogResult = DialogResult.OK;
        }
    }
}
