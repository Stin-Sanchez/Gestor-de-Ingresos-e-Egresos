using GestorIngresosEgresos.Modelo;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace GestorIngresosEgresos.Vista
{
    public partial class FormAbono : Form
    {
        static readonly Color C_SURFACE = Color.White;
        static readonly Color C_BG      = Color.FromArgb(248, 250, 252);
        static readonly Color C_MUTED   = Color.FromArgb(100, 116, 139);
        static readonly Color C_ACCENT  = Color.FromArgb(37, 99, 235);
        static readonly Color C_BORDER  = Color.FromArgb(226, 232, 240);
        static readonly Color C_TEXT    = Color.FromArgb(30, 41, 59);

        private readonly Deuda _deuda;
        private NumericUpDown  _nudMonto;
        private TextBox        _txtNota;

        public decimal Monto { get; private set; }
        public string  Nota  { get; private set; }

        public FormAbono(Deuda deuda)
        {
            _deuda = deuda;
            InitializeComponent();
            ConstruirUI();
        }

        private void ConstruirUI()
        {
            this.Text            = "Registrar Abono";
            this.ClientSize      = new Size(400, 270);
            this.BackColor       = C_SURFACE;
            this.Font            = new Font("Segoe UI", 10F);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;

            // Info banner
            var infoPanel = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = C_BG, Padding = new Padding(20, 10, 20, 10) };
            infoPanel.Paint += (s, e) => e.Graphics.DrawLine(new Pen(C_BORDER), 0, infoPanel.Height - 1, infoPanel.Width, infoPanel.Height - 1);
            infoPanel.Controls.Add(new Label { Text = _deuda.Nombre, Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = C_TEXT, Location = new Point(20, 10), AutoSize = true });
            infoPanel.Controls.Add(new Label { Text = $"Saldo pendiente: ${_deuda.SaldoPendiente:N2}", Font = new Font("Segoe UI", 10F), ForeColor = C_MUTED, Location = new Point(20, 36), AutoSize = true });

            // Fields
            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3,
                Padding = new Padding(20, 14, 20, 14), BackColor = C_SURFACE
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _nudMonto = new NumericUpDown
            {
                Dock = DockStyle.Fill, Minimum = 0.01m, Maximum = _deuda.SaldoPendiente,
                DecimalPlaces = 2, ThousandsSeparator = true,
                Value = Math.Min(100m, _deuda.SaldoPendiente)
            };
            _txtNota = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };

            tlp.Controls.Add(Lbl("Monto ($):"), 0, 0); tlp.Controls.Add(_nudMonto, 1, 0);
            tlp.Controls.Add(Lbl("Nota:"),      0, 1); tlp.Controls.Add(_txtNota,  1, 1);

            var flpBtn = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = C_SURFACE };
            var btnCancelar  = new Button { Text = "Cancelar",  Size = new Size(90, 32),  Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat };
            var btnRegistrar = new Button
            {
                Text = "Registrar", Size = new Size(100, 32), BackColor = C_ACCENT, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnRegistrar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click  += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            btnRegistrar.Click += BtnRegistrar_Click;
            flpBtn.Controls.Add(btnRegistrar);
            flpBtn.Controls.Add(btnCancelar);
            tlp.Controls.Add(flpBtn, 0, 2);
            tlp.SetColumnSpan(flpBtn, 2);

            this.Controls.Add(tlp);
            this.Controls.Add(infoPanel);
            this.AcceptButton = btnRegistrar;
            this.CancelButton = btnCancelar;
        }

        private Label Lbl(string t) => new Label
        {
            Text = t, Dock = DockStyle.Fill, ForeColor = C_MUTED,
            Font = new Font("Segoe UI", 10F), TextAlign = ContentAlignment.MiddleRight
        };

        private void BtnRegistrar_Click(object sender, EventArgs e)
        {
            if (_nudMonto.Value <= 0)
            { MessageBox.Show("El monto debe ser mayor a cero.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            Monto = _nudMonto.Value;
            Nota  = string.IsNullOrWhiteSpace(_txtNota.Text) ? $"Abono a {_deuda.Nombre}" : _txtNota.Text.Trim();
            DialogResult = DialogResult.OK;
        }
    }
}
