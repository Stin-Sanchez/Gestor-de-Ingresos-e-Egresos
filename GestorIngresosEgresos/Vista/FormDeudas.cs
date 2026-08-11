using GestorIngresosEgresos.Controller;
using GestorIngresosEgresos.Modelo;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GestorIngresosEgresos.Vista
{
    public partial class FormDeudas : Form
    {
        static readonly Color C_SIDEBAR = Color.FromArgb(30, 41, 59);
        static readonly Color C_ACCENT  = Color.FromArgb(37, 99, 235);
        static readonly Color C_BG      = Color.FromArgb(248, 250, 252);
        static readonly Color C_SURFACE = Color.White;
        static readonly Color C_BORDER  = Color.FromArgb(226, 232, 240);
        static readonly Color C_TEXT    = Color.FromArgb(30, 41, 59);
        static readonly Color C_MUTED   = Color.FromArgb(100, 116, 139);
        static readonly Color C_INGRESO = Color.FromArgb(16, 185, 129);
        static readonly Color C_GASTO   = Color.FromArgb(239, 68, 68);

        private readonly DeudaController   _ctrl  = new DeudaController();
        private readonly PeriodoController _pCtrl = new PeriodoController();

        static readonly Color C_DEUDA = Color.FromArgb(245, 158, 11);

        private Panel _panelLista;
        private Label _lblActivasVal, _lblPendienteVal, _lblPagadoVal, _lblPctVal;

        public FormDeudas()
        {
            InitializeComponent();
            ConstruirUI();
            CargarDeudas();
        }

        // Needed by Designer.cs Load event wiring (kept minimal)
        private void FormDeudas_Load(object sender, EventArgs e) { }

        private void ConstruirUI()
        {
            this.BackColor = C_BG;
            this.Font      = new Font("Segoe UI", 9.5F);

            // ── Header ──────────────────────────────────────────
            var header = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = C_SIDEBAR, Padding = new Padding(20, 0, 20, 0) };

            var lblTitulo = new Label
            {
                Text      = "Mis Deudas",
                Font      = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Left,
                Width     = 200,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var btnNueva = new Button
            {
                Text      = "+ Nueva Deuda",
                Dock      = DockStyle.Right,
                Width     = 140,
                Font      = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = C_ACCENT,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnNueva.FlatAppearance.BorderSize = 0;
            btnNueva.Click += (s, e) =>
            {
                using (var f = new FormNuevaDeuda())
                    if (f.ShowDialog() == DialogResult.OK)
                    {
                        try { _ctrl.Guardar(f.Resultado); CargarDeudas(); }
                        catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                    }
            };

            header.Controls.Add(btnNueva);
            header.Controls.Add(lblTitulo);

            // ── Dashboard tiles ──────────────────────────────────
            var tilesPanel = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = C_BG, Padding = new Padding(16, 10, 16, 8) };
            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, BackColor = C_BG };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            Panel tA, tP, tPg, tPct;
            _lblActivasVal  = DashTile("DEUDAS ACTIVAS",  C_ACCENT,  out tA);
            _lblPendienteVal = DashTile("TOTAL PENDIENTE", C_GASTO,   out tP);
            _lblPagadoVal   = DashTile("TOTAL PAGADO",    C_INGRESO, out tPg);
            _lblPctVal      = DashTile("% COMPLETADO",    C_DEUDA,   out tPct);
            tP.Margin = tPg.Margin = tPct.Margin = new Padding(8, 0, 0, 0);

            tlp.Controls.Add(tA,   0, 0);
            tlp.Controls.Add(tP,   1, 0);
            tlp.Controls.Add(tPg,  2, 0);
            tlp.Controls.Add(tPct, 3, 0);
            tilesPanel.Controls.Add(tlp);

            // ── Cards list ───────────────────────────────────────
            _panelLista = new Panel
            {
                Dock       = DockStyle.Fill,
                BackColor  = C_BG,
                AutoScroll = true,
                Padding    = new Padding(20, 14, 20, 14)
            };
            _panelLista.HorizontalScroll.Enabled = false;
            _panelLista.HorizontalScroll.Visible = false;

            this.Controls.Add(_panelLista);
            this.Controls.Add(tilesPanel);
            this.Controls.Add(header);
        }

        private void CargarDeudas()
        {
            _panelLista.Controls.Clear();
            var deudas = _ctrl.ObtenerTodas();

            int     activas      = deudas.Count(d => d.Estado == EstadoDeuda.ACTIVA);
            decimal pendiente    = deudas.Where(d => d.Estado == EstadoDeuda.ACTIVA).Sum(d => d.SaldoPendiente);
            decimal totalPagado  = deudas.Sum(d => d.MontoPagado);
            decimal totalOrig    = deudas.Sum(d => d.MontoOriginal);
            int     pctGlobal    = totalOrig > 0 ? (int)(totalPagado * 100 / totalOrig) : 0;

            _lblActivasVal.Text   = activas.ToString();
            _lblPendienteVal.Text = $"${pendiente:N2}";
            _lblPagadoVal.Text    = $"${totalPagado:N2}";
            _lblPctVal.Text       = $"{pctGlobal}%";

            if (!deudas.Any())
            {
                _panelLista.Controls.Add(new Label
                {
                    Text      = "No hay deudas registradas. Haz clic en '+ Nueva Deuda' para agregar una.",
                    Font      = new Font("Segoe UI", 11F),
                    ForeColor = C_MUTED,
                    AutoSize  = true,
                    Location  = new Point(0, 20)
                });
                return;
            }

            int y = 0;
            foreach (var d in deudas.OrderBy(x => x.Estado).ThenByDescending(x => x.FechaInicio))
            {
                var card  = CrearCard(d);
                card.Location = new Point(0, y);
                card.Width    = _panelLista.ClientSize.Width - 2;
                card.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                _panelLista.Controls.Add(card);
                y += card.Height + 10;
            }

            _panelLista.Resize += (s, e) =>
            {
                foreach (Control c in _panelLista.Controls)
                    c.Width = _panelLista.ClientSize.Width - 2;
            };
        }

        private Panel CrearCard(Deuda d)
        {
            bool pagada = d.Estado == EstadoDeuda.PAGADA;
            var card = new Panel { BackColor = C_SURFACE, Height = 148, Padding = new Padding(18, 14, 18, 14) };
            card.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle,
                C_BORDER, 1, ButtonBorderStyle.Solid, C_BORDER, 1, ButtonBorderStyle.Solid,
                C_BORDER, 1, ButtonBorderStyle.Solid, C_BORDER, 1, ButtonBorderStyle.Solid);

            // Nombre
            card.Controls.Add(new Label
            {
                Text = d.Nombre, Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = pagada ? C_MUTED : C_TEXT, Location = new Point(18, 14), AutoSize = true
            });

            // Badge (anchored right)
            var badge = new Label
            {
                Text      = pagada ? "PAGADA" : "ACTIVA",
                Font      = new Font("Segoe UI", 8F, FontStyle.Bold),
                BackColor = pagada ? Color.FromArgb(30, 41, 59) : Color.FromArgb(6, 78, 59),
                ForeColor = pagada ? C_MUTED : C_INGRESO,
                Size      = new Size(65, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            badge.Location = new Point(card.Width - 83, 16);
            card.Controls.Add(badge);

            // Acreedor
            card.Controls.Add(new Label
            {
                Text = $"Acreedor: {d.Acreedor}", Font = new Font("Segoe UI", 10F),
                ForeColor = C_MUTED, Location = new Point(18, 38), AutoSize = true
            });

            // Progress bar
            var progOuter = new Panel
            {
                Location  = new Point(18, 66),
                Height    = 8,
                Width     = card.Width - 36,
                BackColor = C_BORDER,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            var progInner = new Panel { Dock = DockStyle.Left, Width = 0, BackColor = pagada ? C_INGRESO : C_ACCENT };
            progOuter.Controls.Add(progInner);
            progOuter.SizeChanged += (s, e) =>
                progInner.Width = (int)(progOuter.Width * d.PorcentajePagado / 100m);
            card.Controls.Add(progOuter);

            // Amounts
            card.Controls.Add(new Label
            {
                Text = $"Pagado: ${d.MontoPagado:N2}  |  {d.PorcentajePagado}%  |  Pendiente: ${d.SaldoPendiente:N2} / ${d.MontoOriginal:N2}",
                Font = new Font("Segoe UI", 9.5F), ForeColor = C_MUTED,
                Location = new Point(18, 82), AutoSize = true
            });

            // Buttons
            var flp = new FlowLayoutPanel
            {
                Location = new Point(18, 110), Height = 30, Width = 340,
                BackColor = C_SURFACE, WrapContents = false
            };

            if (!pagada)
            {
                var b = CardBtn("Abonar", C_ACCENT);
                b.Click += (s, e) => Abonar(d);
                flp.Controls.Add(b);
            }

            var bHist = CardBtn("Historial", Color.FromArgb(71, 85, 105));
            bHist.Click += (s, e) => { using (var f = new FormHistorialAbonos(d)) f.ShowDialog(); };
            flp.Controls.Add(bHist);

            if (!pagada)
            {
                var bDel = CardBtn("Eliminar", C_GASTO);
                bDel.Click += (s, e) => Eliminar(d);
                flp.Controls.Add(bDel);
            }

            card.Controls.Add(flp);
            return card;
        }

        private Label DashTile(string titulo, Color accentColor, out Panel card)
        {
            card = new Panel { Dock = DockStyle.Fill, BackColor = C_SURFACE };
            var cardRef = card;
            cardRef.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, cardRef.ClientRectangle,
                C_BORDER, 1, ButtonBorderStyle.Solid, C_BORDER, 1, ButtonBorderStyle.Solid,
                C_BORDER, 1, ButtonBorderStyle.Solid, C_BORDER, 1, ButtonBorderStyle.Solid);

            var accent = new Panel { Dock = DockStyle.Top, Height = 3, BackColor = accentColor };
            var lblTit = new Label { Text = titulo, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = C_MUTED, Location = new Point(12, 10), AutoSize = true };
            var lbl    = new Label { Text = "—", Font = new Font("Segoe UI", 17F, FontStyle.Bold), ForeColor = accentColor, Location = new Point(10, 28), AutoSize = true };
            card.Controls.Add(lbl);
            card.Controls.Add(lblTit);
            card.Controls.Add(accent);
            return lbl;
        }

        private Button CardBtn(string t, Color c) => new Button
        {
            Text = t, Size = new Size(88, 28), Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand, Margin = new Padding(0, 0, 8, 0)
        };

        private void Abonar(Deuda d)
        {
            Periodo p = _pCtrl.ObtenerOCrearPeriodo(DateTime.Now.Year, DateTime.Now.Month);
            if (p == null || p.Estado == EstadoPeriodo.CERRADO)
            {
                MessageBox.Show("No hay un periodo abierto para registrar el abono.", "Sin periodo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (var form = new FormAbono(d))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try { _ctrl.RegistrarAbono(d.Id, p.Id, null, form.Monto, form.Nota); CargarDeudas(); }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
            }
        }

        private void Eliminar(Deuda d)
        {
            if (MessageBox.Show($"Eliminar deuda \"{d.Nombre}\"?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try { _ctrl.Eliminar(d.Id); CargarDeudas(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
}
