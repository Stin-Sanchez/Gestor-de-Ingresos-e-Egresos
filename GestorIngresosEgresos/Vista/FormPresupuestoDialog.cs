using GestorIngresosEgresos.Modelo;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GestorIngresosEgresos.Vista
{
    public partial class FormPresupuestoDialog : Form
    {
        static readonly Color C_SURFACE = Color.White;
        static readonly Color C_MUTED   = Color.FromArgb(100, 116, 139);
        static readonly Color C_TEXT    = Color.FromArgb(30, 41, 59);
        static readonly Color C_ACCENT  = Color.FromArgb(37, 99, 235);

        private readonly int  _periodoId;
        private readonly bool _esEdicion;
        private readonly int  _existingId;
        private readonly int  _categoriaId;

        private ComboBox      _cboCategoria;
        private NumericUpDown _nudMonto;

        public Presupuesto Resultado { get; private set; }

        // Modo crear
        public FormPresupuestoDialog(int periodoId, List<CategoriaGasto> categoriasDisponibles)
        {
            _periodoId = periodoId;
            _esEdicion = false;
            InitializeComponent();
            ConstruirUI(categoriasDisponibles, null, 0m);
        }

        // Modo editar
        public FormPresupuestoDialog(int periodoId, PresupuestoResumen existing)
        {
            _periodoId   = periodoId;
            _esEdicion   = true;
            _existingId  = existing.Id;
            _categoriaId = existing.CategoriaId;
            InitializeComponent();
            ConstruirUI(null, existing.CategoriaNombre, existing.Limite);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "FormPresupuestoDialog";
            this.ResumeLayout(false);
        }

        private void ConstruirUI(List<CategoriaGasto> categoriasDisponibles, string categoriaNombreFija, decimal montoActual)
        {
            this.Text            = _esEdicion ? "Editar Presupuesto" : "Nuevo Presupuesto";
            this.ClientSize      = new Size(360, 190);
            this.BackColor       = C_SURFACE;
            this.Font            = new Font("Segoe UI", 10F);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;

            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3,
                Padding = new Padding(24, 20, 24, 16), BackColor = C_SURFACE
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 2; i++) tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Control categoriaControl;
            if (_esEdicion)
            {
                categoriaControl = new Label
                {
                    Text = categoriaNombreFija, Dock = DockStyle.Fill, ForeColor = C_TEXT,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft
                };
            }
            else
            {
                _cboCategoria = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
                foreach (var c in categoriasDisponibles) _cboCategoria.Items.Add(c);
                if (_cboCategoria.Items.Count > 0) _cboCategoria.SelectedIndex = 0;
                categoriaControl = _cboCategoria;
            }

            _nudMonto = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0.01m, Maximum = 9999999m, DecimalPlaces = 2, ThousandsSeparator = true };
            if (_esEdicion) _nudMonto.Value = montoActual;

            tlp.Controls.Add(Lbl("Categoria:"), 0, 0); tlp.Controls.Add(categoriaControl, 1, 0);
            tlp.Controls.Add(Lbl("Monto ($):"), 0, 1); tlp.Controls.Add(_nudMonto,        1, 1);

            var flpBtn = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = C_SURFACE };
            var btnCancelar = new Button { Text = "Cancelar", Size = new Size(90, 32), Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat };
            var btnGuardar  = new Button
            {
                Text = "Guardar", Size = new Size(90, 32), BackColor = C_ACCENT, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            btnGuardar.Click  += BtnGuardar_Click;
            flpBtn.Controls.Add(btnGuardar);
            flpBtn.Controls.Add(btnCancelar);
            tlp.Controls.Add(flpBtn, 0, 2);
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
            if (!_esEdicion && _cboCategoria.SelectedItem == null)
            { MessageBox.Show("Selecciona una categoria.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (_nudMonto.Value <= 0)
            { MessageBox.Show("El monto debe ser mayor a cero.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            int categoriaId = _esEdicion ? _categoriaId : ((CategoriaGasto)_cboCategoria.SelectedItem).Id;

            Resultado = new Presupuesto
            {
                Id          = _existingId,
                PeriodoId   = _periodoId,
                CategoriaId = categoriaId,
                Monto       = _nudMonto.Value
            };
            DialogResult = DialogResult.OK;
        }
    }
}
