using GestorIngresosEgresos.Controller;
using GestorIngresosEgresos.Modelo;
using System.Drawing;
using System.Windows.Forms;

namespace GestorIngresosEgresos.Vista
{
    public partial class FormHistorialAbonos : Form
    {
        static readonly Color C_BG      = Color.FromArgb(248, 250, 252);
        static readonly Color C_SURFACE = Color.White;
        static readonly Color C_BORDER  = Color.FromArgb(226, 232, 240);
        static readonly Color C_TEXT    = Color.FromArgb(30, 41, 59);
        static readonly Color C_MUTED   = Color.FromArgb(100, 116, 139);
        static readonly Color C_ACCENT  = Color.FromArgb(37, 99, 235);

        private readonly Deuda           _deuda;
        private readonly GastoController _ctrl = new GastoController();

        public FormHistorialAbonos(Deuda deuda)
        {
            _deuda = deuda;
            InitializeComponent();
            ConstruirUI();
        }

        private void ConstruirUI()
        {
            this.Text            = $"Historial — {_deuda.Nombre}";
            this.ClientSize      = new Size(580, 440);
            this.BackColor       = C_BG;
            this.Font            = new Font("Segoe UI", 10F);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;

            var header = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = C_SURFACE, Padding = new Padding(16, 0, 16, 0) };
            header.Paint += (s, e) => e.Graphics.DrawLine(new System.Drawing.Pen(C_BORDER), 0, header.Height - 1, header.Width, header.Height - 1);
            header.Controls.Add(new Label
            {
                Text      = $"{_deuda.Nombre}  —  Abonado: ${_deuda.MontoPagado:N2}  |  Pendiente: ${_deuda.SaldoPendiente:N2}",
                Font      = new Font("Segoe UI", 10F),
                ForeColor = C_TEXT,
                Dock      = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            });

            var gridPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 10, 16, 16), BackColor = C_BG };
            var dgv = new DataGridView
            {
                Dock                      = DockStyle.Fill,
                BackgroundColor           = C_SURFACE,
                BorderStyle               = BorderStyle.None,
                AllowUserToAddRows        = false,
                AllowUserToDeleteRows     = false,
                ReadOnly                  = true,
                SelectionMode             = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible         = false,
                AutoSizeColumnsMode       = DataGridViewAutoSizeColumnsMode.Fill,
                Font                      = new Font("Segoe UI", 10F),
                ColumnHeadersHeight       = 36,
                EnableHeadersVisualStyles = false,
                CellBorderStyle           = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor                 = C_BORDER
            };
            dgv.RowTemplate.Height = 40;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = C_BG;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = C_MUTED;
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgv.DefaultCellStyle.SelectionBackColor      = Color.FromArgb(241, 245, 249);
            dgv.DefaultCellStyle.SelectionForeColor      = C_TEXT;
            dgv.DefaultCellStyle.Padding                 = new Padding(8, 0, 8, 0);

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fecha",       HeaderText = "FECHA",       FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Descripcion", HeaderText = "DESCRIPCION", FillWeight = 55 });
            var colMonto = new DataGridViewTextBoxColumn { Name = "Monto", HeaderText = "MONTO", FillWeight = 25 };
            colMonto.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            colMonto.DefaultCellStyle.Font      = new Font("Segoe UI", 10F, FontStyle.Bold);
            colMonto.DefaultCellStyle.ForeColor  = C_ACCENT;
            dgv.Columns.Add(colMonto);

            var abonos = _ctrl.ObtenerAbonosPorDeuda(_deuda.Id);
            foreach (var a in abonos)
                dgv.Rows.Add(a.Fecha.ToString("dd/MM/yyyy"), a.Descripcion, $"${a.Monto:N2}");
            if (abonos.Count == 0)
                dgv.Rows.Add("—", "Sin abonos registrados", "$0.00");

            gridPanel.Controls.Add(dgv);
            this.Controls.Add(gridPanel);
            this.Controls.Add(header);
        }
    }
}
