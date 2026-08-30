using GestorIngresosEgresos.Controller;
using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GestorIngresosEgresos.Vista
{
    public partial class FormPeriodo : Form
    {
        static readonly Color C_SIDEBAR = Color.FromArgb(30, 41, 59);
        static readonly Color C_BG      = Color.FromArgb(248, 250, 252);
        static readonly Color C_SURFACE = Color.White;
        static readonly Color C_BORDER  = Color.FromArgb(226, 232, 240);
        static readonly Color C_TEXT    = Color.FromArgb(30, 41, 59);
        static readonly Color C_MUTED   = Color.FromArgb(100, 116, 139);
        static readonly Color C_INGRESO = Color.FromArgb(16, 185, 129);
        static readonly Color C_GASTO   = Color.FromArgb(239, 68, 68);

        private readonly PeriodoController _periodoCtrl = new PeriodoController();
        private readonly IngresoController _ingresoCtrl = new IngresoController();
        private readonly GastoController   _gastoCtrl   = new GastoController();

        private Periodo _periodo;

        private Label  _lblNombre, _lblEstado, _lblSueldo;
        private Button _btnAnterior, _btnSiguiente, _btnEditarSueldo;
        private Label  _lblSaldo, _lblIngresos, _lblGastos;
        private Button _btnIngreso, _btnGasto;
        private TextBox  _txtBuscar;
        private ComboBox _cboFiltro;
        private DataGridView _dgv;
        private List<FilaMovimiento> _filas = new List<FilaMovimiento>();

        private class FilaMovimiento
        {
            public bool     EsIngreso   { get; set; }
            public bool     EsAbono     { get; set; }
            public bool     EsSobre     { get; set; }
            public int      Id          { get; set; }
            public int?     CategoriaId { get; set; }
            public DateTime Fecha       { get; set; }
            public string   Tipo        { get; set; }
            public string   Descripcion { get; set; }
            public decimal  Monto       { get; set; }
        }

        public FormPeriodo()
        {
            InitializeComponent();
            ConstruirUI();
            CargarPeriodo();
        }

        private void ConstruirUI()
        {
            this.BackColor = C_BG;
            this.Font      = new Font("Segoe UI", 9.5F);

            // ── Header ──────────────────────────────────────────
            var header = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = C_SIDEBAR };

            _btnAnterior  = NavBtn("<");
            _btnSiguiente = NavBtn(">");
            _btnAnterior.Location  = new Point(10, 13);
            _btnSiguiente.Location = new Point(220, 13);

            _lblNombre = new Label
            {
                Location  = new Point(46, 15),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White
            };
            _lblEstado = new Label
            {
                Location  = new Point(258, 17),
                Size      = new Size(72, 22),
                AutoSize  = false,
                Font      = new Font("Segoe UI", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _lblSueldo = new Label
            {
                Location  = new Point(350, 18),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(148, 163, 184)
            };
            _btnEditarSueldo = new Button
            {
                Text      = "Editar",
                Size      = new Size(54, 22),
                Location  = new Point(560, 17),
                Font      = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            _btnEditarSueldo.FlatAppearance.BorderSize = 0;

            _btnAnterior.Click     += (s, e) => { PeriodoManager.IrAnterior();  CargarPeriodo(); };
            _btnSiguiente.Click    += (s, e) => { PeriodoManager.IrSiguiente(); CargarPeriodo(); };
            _btnEditarSueldo.Click += BtnEditarSueldo_Click;

            header.Controls.AddRange(new Control[] { _btnAnterior, _lblNombre, _btnSiguiente, _lblEstado, _lblSueldo, _btnEditarSueldo });

            // ── Tiles ────────────────────────────────────────────
            var tilesPanel = new Panel { Dock = DockStyle.Top, Height = 105, BackColor = C_BG, Padding = new Padding(14, 10, 14, 8) };
            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = C_BG };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            Panel tSaldo, tIng, tGas;
            _lblSaldo    = Tile("SALDO DISPONIBLE", out tSaldo);
            _lblIngresos = Tile("TOTAL INGRESOS",   out tIng);
            _lblGastos   = Tile("TOTAL GASTOS",     out tGas);
            tIng.Margin  = new Padding(8, 0, 8, 0);
            tlp.Controls.Add(tSaldo, 0, 0);
            tlp.Controls.Add(tIng,   1, 0);
            tlp.Controls.Add(tGas,   2, 0);
            tilesPanel.Controls.Add(tlp);

            // ── Controls bar ──────────────────────────────────────
            var barPanel = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = C_SURFACE, Padding = new Padding(14, 10, 14, 10) };
            _btnIngreso = AccBtn("+ Ingreso", C_INGRESO);
            _btnGasto   = AccBtn("+ Gasto",   C_GASTO);
            _txtBuscar  = new TextBox { Text = "Buscar...", ForeColor = C_MUTED, Font = new Font("Segoe UI", 10F), Width = 200, BorderStyle = BorderStyle.FixedSingle };
            _txtBuscar.GotFocus  += (s, e) => { if (_txtBuscar.Text == "Buscar...") { _txtBuscar.Text = ""; _txtBuscar.ForeColor = C_TEXT; } };
            _txtBuscar.LostFocus += (s, e) => { if (string.IsNullOrEmpty(_txtBuscar.Text)) { _txtBuscar.Text = "Buscar..."; _txtBuscar.ForeColor = C_MUTED; } };
            _cboFiltro  = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), Width = 110 };
            _cboFiltro.Items.AddRange(new object[] { "Todo", "Ingresos", "Gastos" });
            _cboFiltro.SelectedIndex = 0;

            _btnIngreso.Click += BtnIngreso_Click;
            _btnGasto.Click   += BtnGasto_Click;
            _txtBuscar.TextChanged += (s, e) => { if (_txtBuscar.Text != "Buscar...") Filtrar(); };
            _cboFiltro.SelectedIndexChanged += (s, e) => Filtrar();

            var flp = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = C_SURFACE, WrapContents = false };
            _btnIngreso.Margin = new Padding(0, 1, 8, 0);
            _btnGasto.Margin   = new Padding(0, 1, 18, 0);
            _txtBuscar.Margin  = new Padding(0, 2, 8, 0);
            _cboFiltro.Margin  = new Padding(0, 2, 0, 0);
            flp.Controls.AddRange(new Control[] { _btnIngreso, _btnGasto, _txtBuscar, _cboFiltro });
            barPanel.Controls.Add(flp);

            // ── Grid ──────────────────────────────────────────────
            var gridPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 6, 14, 14), BackColor = C_BG };
            _dgv = new DataGridView
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
                ColumnHeadersHeight       = 38,
                EnableHeadersVisualStyles = false,
                CellBorderStyle           = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor                 = C_BORDER
            };
            _dgv.RowTemplate.Height = 44;
            _dgv.ColumnHeadersDefaultCellStyle.BackColor = C_BG;
            _dgv.ColumnHeadersDefaultCellStyle.ForeColor = C_MUTED;
            _dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            _dgv.ColumnHeadersDefaultCellStyle.Padding   = new Padding(8, 0, 0, 0);
            _dgv.DefaultCellStyle.SelectionBackColor      = Color.FromArgb(241, 245, 249);
            _dgv.DefaultCellStyle.SelectionForeColor      = C_TEXT;
            _dgv.DefaultCellStyle.Padding                 = new Padding(8, 0, 8, 0);

            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fecha",       HeaderText = "FECHA",       FillWeight = 10 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tipo",        HeaderText = "TIPO",        FillWeight = 12 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Descripcion", HeaderText = "DESCRIPCION", FillWeight = 50 });
            var colMonto = new DataGridViewTextBoxColumn { Name = "Monto", HeaderText = "MONTO", FillWeight = 14 };
            colMonto.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            colMonto.DefaultCellStyle.Font      = new Font("Segoe UI", 10F, FontStyle.Bold);
            _dgv.Columns.Add(colMonto);
            var colAcc = new DataGridViewTextBoxColumn { Name = "Acc", HeaderText = "", FillWeight = 5 };
            colAcc.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colAcc.DefaultCellStyle.ForeColor  = C_MUTED;
            _dgv.Columns.Add(colAcc);

            _dgv.CellFormatting += Dgv_CellFormatting;
            _dgv.CellClick      += Dgv_CellClick;
            _dgv.CellMouseEnter += (s, e) => { if (e.RowIndex >= 0 && _dgv.Columns[e.ColumnIndex].Name == "Acc") _dgv.Cursor = Cursors.Hand; };
            _dgv.CellMouseLeave += (s, e) => _dgv.Cursor = Cursors.Default;
            gridPanel.Controls.Add(_dgv);

            // Add in reverse order for correct Dock=Top stacking
            this.Controls.Add(gridPanel);
            this.Controls.Add(barPanel);
            this.Controls.Add(tilesPanel);
            this.Controls.Add(header);
        }

        private Button NavBtn(string t) => new Button
        {
            Text = t, Size = new Size(30, 30),
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 163, 184), BackColor = C_SIDEBAR,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };

        private Button AccBtn(string t, Color c) => new Button
        {
            Text = t, Size = new Size(108, 30),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };

        private Label Tile(string titulo, out Panel card)
        {
            card = new Panel { Dock = DockStyle.Fill, BackColor = C_SURFACE };
            var cardRef = card;
            cardRef.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, cardRef.ClientRectangle,
                C_BORDER, 1, ButtonBorderStyle.Solid, C_BORDER, 1, ButtonBorderStyle.Solid,
                C_BORDER, 1, ButtonBorderStyle.Solid, C_BORDER, 1, ButtonBorderStyle.Solid);
            card.Controls.Add(new Label { Text = titulo, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = C_MUTED, Location = new Point(14, 12), AutoSize = true });
            var lbl = new Label { Text = "$0.00", Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = C_TEXT, Location = new Point(12, 34), AutoSize = true };
            card.Controls.Add(lbl);
            return lbl;
        }

        // ── Data ──────────────────────────────────────────────────

        private void CargarPeriodo()
        {
            _periodo = _periodoCtrl.ObtenerOCrearPeriodo(PeriodoManager.Anio, PeriodoManager.Mes);

            _lblNombre.Text = _periodo?.Nombre ?? Capitalizar(PeriodoManager.NombrePeriodo);
            bool sinDatos = _periodo == null;
            bool abierto  = _periodo?.Estado == EstadoPeriodo.ABIERTO;

            _lblEstado.Text      = sinDatos ? "SIN DATOS" : (abierto ? "ABIERTO" : "CERRADO");
            _lblEstado.BackColor = sinDatos ? Color.FromArgb(51, 65, 85) : (abierto ? Color.FromArgb(6, 78, 59) : Color.FromArgb(69, 26, 3));
            _lblEstado.ForeColor = sinDatos ? C_MUTED : (abierto ? C_INGRESO : Color.FromArgb(245, 158, 11));
            _lblSueldo.Text      = sinDatos ? "" : $"Sueldo: ${_periodo.SueldoBase:N2}";
            _btnIngreso.Enabled  = abierto;
            _btnGasto.Enabled    = abierto;
            _btnEditarSueldo.Enabled = abierto;

            if (sinDatos) { MostrarFilas(new List<FilaMovimiento>()); ActualizarTiles(0, 0); return; }
            CargarTabla();
        }

        private void CargarTabla()
        {
            var ingresos = _ingresoCtrl.ObtenerPorPeriodo(_periodo.Id);
            var gastos   = _gastoCtrl.ObtenerPorPeriodo(_periodo.Id);

            _filas = ingresos.Select(i => new FilaMovimiento
            {
                EsIngreso = true, EsAbono = false, Id = i.Id, Fecha = i.Fecha,
                Tipo = Capitalizar(i.Tipo.ToString().ToLower()), Descripcion = i.Descripcion, Monto = i.Monto
            }).Concat(gastos.Select(g => new FilaMovimiento
            {
                EsIngreso = false, EsAbono = g.EsAbono, EsSobre = g.EsSobre, Id = g.Id, CategoriaId = g.CategoriaId, Fecha = g.Fecha,
                // El sobre se marca en Tipo, no en Descripcion: Descripcion se reusa tal cual al editar.
                Tipo = g.EsAbono ? "Abono"
                     : (g.EsSobre ? "Sobre" : (string.IsNullOrEmpty(g.CategoriaNombre) ? "Gasto" : g.CategoriaNombre)),
                Descripcion = g.EsAbono ? $"Abono: {g.Descripcion}" : g.Descripcion, Monto = g.Monto
            })).OrderByDescending(f => f.Fecha).ToList();

            ActualizarTiles(ingresos.Sum(i => i.Monto), gastos.Sum(g => g.Monto));
            Filtrar();
        }

        private void ActualizarTiles(decimal ing, decimal gas)
        {
            decimal s = ing - gas;
            _lblSaldo.Text     = $"${s:N2}";  _lblSaldo.ForeColor    = s >= 0 ? C_INGRESO : C_GASTO;
            _lblIngresos.Text  = $"${ing:N2}"; _lblIngresos.ForeColor = C_INGRESO;
            _lblGastos.Text    = $"${gas:N2}"; _lblGastos.ForeColor   = C_GASTO;
        }

        private void Filtrar()
        {
            string q = (_txtBuscar.Text == "Buscar...") ? "" : _txtBuscar.Text.Trim().ToLower();
            int    f = _cboFiltro.SelectedIndex;
            var lista = _filas.Where(x =>
            {
                if (f == 1 && !x.EsIngreso) return false;
                if (f == 2 &&  x.EsIngreso) return false;
                return string.IsNullOrEmpty(q) || x.Descripcion.ToLower().Contains(q) || x.Tipo.ToLower().Contains(q);
            }).ToList();
            MostrarFilas(lista);
        }

        private void FormPeriodo_Load(object sender, EventArgs e)
        {

        }

        private void MostrarFilas(List<FilaMovimiento> lista)
        {
            _dgv.Rows.Clear();
            foreach (var f in lista)
            {
                int idx = _dgv.Rows.Add(f.Fecha.ToString("dd/MM/yy"), f.Tipo, f.Descripcion,
                    f.EsIngreso ? $"+${f.Monto:N2}" : $"-${f.Monto:N2}", "...");
                _dgv.Rows[idx].Tag = f;
            }
            _dgv.ClearSelection();
        }

        // ── Events ────────────────────────────────────────────────

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || _dgv.Rows[e.RowIndex].Tag == null) return;
            var f = (FilaMovimiento)_dgv.Rows[e.RowIndex].Tag;
            string col = _dgv.Columns[e.ColumnIndex].Name;
            if (col == "Monto" || col == "Tipo")
                e.CellStyle.ForeColor = f.EsIngreso ? C_INGRESO : C_GASTO;
        }

        private void Dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _dgv.Columns[e.ColumnIndex].Name != "Acc") return;
            var f = (FilaMovimiento)_dgv.Rows[e.RowIndex].Tag;
            if (_periodo?.Estado != EstadoPeriodo.ABIERTO) return;

            var menu = new ContextMenuStrip { Font = new Font("Segoe UI", 10F) };
            if (!f.EsAbono)
                menu.Items.Add("Editar").Click += (s, ev) => EditarFila(f);
            menu.Items.Add("Eliminar").Click += (s, ev) => EliminarFila(f);
            var rect = _dgv.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            menu.Show(_dgv, rect.Left, rect.Bottom);
        }

        private void EditarFila(FilaMovimiento f)
        {
            try
            {
                if (f.EsIngreso)
                {
                    var existing = new Ingreso
                    {
                        Id = f.Id, PeriodoId = _periodo.Id, Fecha = f.Fecha,
                        Tipo = (TipoIngreso)Enum.Parse(typeof(TipoIngreso), f.Tipo.ToUpper()),
                        Descripcion = f.Descripcion, Monto = f.Monto
                    };
                    using (var form = new FormIngreso(_periodo.Id, existing))
                        if (form.ShowDialog() == DialogResult.OK)
                            { _ingresoCtrl.Actualizar(form.Resultado); CargarTabla(); }
                }
                else
                {
                    var existing = new Gasto
                    {
                        Id = f.Id, PeriodoId = _periodo.Id, CategoriaId = f.CategoriaId,
                        Fecha = f.Fecha, Descripcion = f.Descripcion, Monto = f.Monto, EsSobre = f.EsSobre
                    };
                    using (var form = new FormGasto(_periodo.Id, _gastoCtrl.ObtenerCategorias(), existing))
                        if (form.ShowDialog() == DialogResult.OK)
                            { _gastoCtrl.Actualizar(form.Resultado); CargarTabla(); }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void EliminarFila(FilaMovimiento f)
        {
            if (MessageBox.Show($"Eliminar \"{f.Descripcion}\"?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                if (f.EsIngreso) _ingresoCtrl.Eliminar(f.Id);
                else             _gastoCtrl.Eliminar(f.Id);
                CargarTabla();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void BtnIngreso_Click(object sender, EventArgs e)
        {
            using (var form = new FormIngreso(_periodo.Id))
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try { _ingresoCtrl.Guardar(form.Resultado); CargarTabla(); }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
        }

        private void BtnGasto_Click(object sender, EventArgs e)
        {
            using (var form = new FormGasto(_periodo.Id, _gastoCtrl.ObtenerCategorias()))
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try { _gastoCtrl.Guardar(form.Resultado); CargarTabla(); }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
        }

        private void BtnEditarSueldo_Click(object sender, EventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox(
                $"Sueldo base para {_periodo.Nombre}:", "Editar sueldo base",
                _periodo.SueldoBase.ToString("N2"));
            if (!decimal.TryParse(input.Replace(",", ""), out decimal v)) return;
            try
            {
                _periodoCtrl.ActualizarSueldoBase(_periodo.Id, v);
                _periodo.SueldoBase = v;
                _lblSueldo.Text = $"Sueldo: ${v:N2}";
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private static string Capitalizar(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
    }
}
