using GestorIngresosEgresos.Modelo;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GestorIngresosEgresos.Vista
{
    public partial class FormGasto : Form
    {
        static readonly Color C_SURFACE = Color.White;
        static readonly Color C_MUTED   = Color.FromArgb(100, 116, 139);
        static readonly Color C_GASTO   = Color.FromArgb(239, 68, 68);

        private readonly int                  _periodoId;
        private readonly List<CategoriaGasto> _categorias;
        private readonly int?                 _existingId;

        private DateTimePicker _dtpFecha;
        private ComboBox       _cboCategoria;
        private TextBox        _txtDescripcion;
        private NumericUpDown  _nudMonto;
        private CheckBox       _chkSobre;

        public Gasto Resultado { get; private set; }

        public FormGasto(int periodoId, List<CategoriaGasto> categorias, Gasto existing = null)
        {
            _periodoId  = periodoId;
            _categorias = categorias;
            _existingId = existing?.Id;
            InitializeComponent();
            ConstruirUI();
            if (existing != null)
            {
                this.Text            = "Editar Gasto";
                _dtpFecha.Value      = existing.Fecha;
                _txtDescripcion.Text = existing.Descripcion;
                _nudMonto.Value      = existing.Monto;
                _chkSobre.Checked    = existing.EsSobre;
                // Seleccionar categoría por Id
                for (int i = 0; i < _cboCategoria.Items.Count; i++)
                {
                    var item = (CategoriaItem)_cboCategoria.Items[i];
                    if (item.Id == existing.CategoriaId) { _cboCategoria.SelectedIndex = i; break; }
                }
            }
        }

        private void ConstruirUI()
        {
            this.Text            = "Nuevo Gasto";
            this.ClientSize      = new Size(420, 356);
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
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 4; i++) tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _dtpFecha       = new DateTimePicker { Dock = DockStyle.Fill, Value = DateTime.Today, Format = DateTimePickerFormat.Short };
            _cboCategoria   = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _txtDescripcion = new TextBox  { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            _nudMonto       = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0.01m, Maximum = 9999999m, DecimalPlaces = 2, ThousandsSeparator = true };

            _cboCategoria.Items.Add(new CategoriaItem { Id = null, Nombre = "(Sin categoria)" });
            foreach (var c in _categorias)
                _cboCategoria.Items.Add(new CategoriaItem { Id = c.Id, Nombre = c.Nombre });
            _cboCategoria.SelectedIndex = 0;

            tlp.Controls.Add(Lbl("Fecha:"),       0, 0); tlp.Controls.Add(_dtpFecha,       1, 0);
            tlp.Controls.Add(Lbl("Categoria:"),   0, 1); tlp.Controls.Add(_cboCategoria,   1, 1);
            tlp.Controls.Add(Lbl("Descripcion:"), 0, 2); tlp.Controls.Add(_txtDescripcion, 1, 2);
            tlp.Controls.Add(Lbl("Monto ($):"),   0, 3); tlp.Controls.Add(_nudMonto,       1, 3);

            _chkSobre = new CheckBox
            {
                Text = "Es un sobre: lo voy consumiendo durante el mes",
                Dock = DockStyle.Fill, ForeColor = C_MUTED, Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand, AutoSize = false
            };
            tlp.Controls.Add(_chkSobre, 1, 4);

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
            if (string.IsNullOrWhiteSpace(_txtDescripcion.Text))
            { MessageBox.Show("La descripcion es obligatoria.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (_nudMonto.Value <= 0)
            { MessageBox.Show("El monto debe ser mayor a cero.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            var cat = (CategoriaItem)_cboCategoria.SelectedItem;
            Resultado = new Gasto
            {
                Id          = _existingId ?? 0,
                PeriodoId   = _periodoId,
                CategoriaId = cat.Id,
                Fecha       = _dtpFecha.Value.Date,
                Descripcion = _txtDescripcion.Text.Trim(),
                Monto       = _nudMonto.Value,
                EsSobre     = _chkSobre.Checked
            };
            DialogResult = DialogResult.OK;
        }

        private class CategoriaItem
        {
            public int?   Id     { get; set; }
            public string Nombre { get; set; }
            public override string ToString() => Nombre;
        }
    }
}
