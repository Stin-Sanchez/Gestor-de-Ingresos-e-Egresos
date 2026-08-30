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
    // Sobres del periodo a la izquierda; consumos del sobre seleccionado a la derecha.
    public partial class FormPresupuestos : Form
    {
        static readonly Color C_SIDEBAR  = Color.FromArgb(30, 41, 59);
        static readonly Color C_BG       = Color.FromArgb(248, 250, 252);
        static readonly Color C_SURFACE  = Color.White;
        static readonly Color C_BORDER   = Color.FromArgb(226, 232, 240);
        static readonly Color C_TEXT     = Color.FromArgb(30, 41, 59);
        static readonly Color C_MUTED    = Color.FromArgb(100, 116, 139);
        static readonly Color C_OK       = Color.FromArgb(16, 185, 129);
        static readonly Color C_ALERTA   = Color.FromArgb(245, 158, 11);
        static readonly Color C_CRITICO  = Color.FromArgb(234, 88, 12);
        static readonly Color C_EXCEDIDO = Color.FromArgb(239, 68, 68);
        static readonly Color C_SEL      = Color.FromArgb(239, 246, 255);

        private readonly PeriodoController     _periodoCtrl     = new PeriodoController();
        private readonly PresupuestoController _presupuestoCtrl = new PresupuestoController();

        private Periodo _periodo;
        private List<PresupuestoResumen> _sobres = new List<PresupuestoResumen>();
        private int? _sobreSeleccionadoId;

        private Label  _lblNombre, _lblEstado, _lblVacio, _lblDetalleTitulo, _lblDetalleResumen;
        private Button _btnAnterior, _btnSiguiente, _btnNuevoConsumo;
        private FlowLayoutPanel _panelTarjetas;
        private DataGridView _dgvConsumos;

        public FormPresupuestos()
        {
            InitializeComponent();
            ConstruirUI();
            CargarPeriodo();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "FormPresupuestos";
            this.ResumeLayout(false);
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
                Location = new Point(46, 15), AutoSize = true,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = Color.White
            };
            _lblEstado = new Label
            {
                Location = new Point(258, 17), Size = new Size(72, 22), AutoSize = false,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter
            };

            _btnAnterior.Click  += (s, e) => { PeriodoManager.IrAnterior();  CargarPeriodo(); };
            _btnSiguiente.Click += (s, e) => { PeriodoManager.IrSiguiente(); CargarPeriodo(); };

            header.Controls.AddRange(new Control[] { _btnAnterior, _lblNombre, _btnSiguiente, _lblEstado });

            // ── Detalle (derecha): consumos del sobre seleccionado ──
            var panelDetalle = new Panel { Dock = DockStyle.Fill, BackColor = C_BG, Padding = new Padding(0, 14, 14, 14) };

            var barraDetalle = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = C_SURFACE };
            _lblDetalleTitulo = new Label
            {
                Location = new Point(14, 10), Size = new Size(400, 22), AutoEllipsis = true, ForeColor = C_TEXT,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold), Text = "Selecciona un sobre"
            };
            _lblDetalleResumen = new Label
            {
                Location = new Point(14, 34), Size = new Size(400, 18), AutoEllipsis = true, ForeColor = C_MUTED,
                Font = new Font("Segoe UI", 9F), Text = ""
            };
            _btnNuevoConsumo = new Button
            {
                Text = "+ Consumo", Size = new Size(120, 30), Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = C_EXCEDIDO, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Right, Enabled = false
            };
            _btnNuevoConsumo.FlatAppearance.BorderSize = 0;
            _btnNuevoConsumo.Click += BtnNuevoConsumo_Click;
            barraDetalle.Resize += (s, e) => _btnNuevoConsumo.Location = new Point(Math.Max(430, barraDetalle.Width - 134), 16);
            barraDetalle.Controls.AddRange(new Control[] { _lblDetalleTitulo, _lblDetalleResumen, _btnNuevoConsumo });

            _dgvConsumos = new DataGridView
            {
                Dock                      = DockStyle.Fill,
                BackgroundColor           = C_SURFACE,
                BorderStyle               = BorderStyle.None,
                AllowUserToAddRows        = false,
                AllowUserToDeleteRows     = false,
                AllowUserToResizeRows     = false,
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
            _dgvConsumos.RowTemplate.Height = 40;
            _dgvConsumos.ColumnHeadersDefaultCellStyle.BackColor = C_BG;
            _dgvConsumos.ColumnHeadersDefaultCellStyle.ForeColor = C_MUTED;
            _dgvConsumos.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9F, FontStyle.Bold);
            _dgvConsumos.DefaultCellStyle.SelectionBackColor     = Color.FromArgb(241, 245, 249);
            _dgvConsumos.DefaultCellStyle.SelectionForeColor     = C_TEXT;
            _dgvConsumos.DefaultCellStyle.Padding                = new Padding(8, 0, 8, 0);

            _dgvConsumos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fecha", HeaderText = "FECHA", FillWeight = 16 });
            _dgvConsumos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Descripcion", HeaderText = "EN QUE", FillWeight = 54 });
            var colMonto = new DataGridViewTextBoxColumn { Name = "Monto", HeaderText = "MONTO", FillWeight = 20 };
            colMonto.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            colMonto.DefaultCellStyle.Font      = new Font("Segoe UI", 10F, FontStyle.Bold);
            colMonto.DefaultCellStyle.ForeColor = C_EXCEDIDO;
            _dgvConsumos.Columns.Add(colMonto);
            var colAcc = new DataGridViewTextBoxColumn { Name = "Acc", HeaderText = "", FillWeight = 10 };
            colAcc.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colAcc.DefaultCellStyle.ForeColor = C_MUTED;
            _dgvConsumos.Columns.Add(colAcc);
            _dgvConsumos.CellClick += DgvConsumos_CellClick;

            panelDetalle.Controls.Add(_dgvConsumos);
            panelDetalle.Controls.Add(barraDetalle);

            // ── Sobres (izquierda) ───────────────────────────────
            var panelIzq = new Panel { Dock = DockStyle.Left, Width = 320, BackColor = C_BG, Padding = new Padding(14, 14, 0, 14) };
            _panelTarjetas = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = C_BG };
            _lblVacio = new Label
            {
                Text = "No hay sobres este mes.\r\n\r\nEn \"Ingresos y Egresos\" crea un egreso\r\ny marca \"Es un sobre\" para irlo\r\nconsumiendo aqui.",
                AutoSize = true, ForeColor = C_MUTED, Font = new Font("Segoe UI", 9.5F),
                Location = new Point(6, 6), Visible = false
            };
            panelIzq.Controls.Add(_lblVacio);
            panelIzq.Controls.Add(_panelTarjetas);

            this.Controls.Add(panelDetalle);
            this.Controls.Add(panelIzq);
            this.Controls.Add(header);
        }

        private Button NavBtn(string t) => new Button
        {
            Text = t, Size = new Size(30, 30), Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 163, 184), BackColor = C_SIDEBAR, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };

        // ── Carga ─────────────────────────────────────────────────

        private void CargarPeriodo()
        {
            _periodo = _periodoCtrl.ObtenerOCrearPeriodo(PeriodoManager.Anio, PeriodoManager.Mes);

            _lblNombre.Text = _periodo?.Nombre ?? Capitalizar(PeriodoManager.NombrePeriodo);
            bool sinDatos = _periodo == null;
            bool abierto  = _periodo?.Estado == EstadoPeriodo.ABIERTO;

            _lblEstado.Text      = sinDatos ? "SIN DATOS" : (abierto ? "ABIERTO" : "CERRADO");
            _lblEstado.BackColor = sinDatos ? Color.FromArgb(51, 65, 85) : (abierto ? Color.FromArgb(6, 78, 59) : Color.FromArgb(69, 26, 3));
            _lblEstado.ForeColor = sinDatos ? C_MUTED : (abierto ? C_OK : Color.FromArgb(245, 158, 11));

            _sobreSeleccionadoId = null;
            if (sinDatos)
            {
                _sobres = new List<PresupuestoResumen>();
                MostrarTarjetas();
                MostrarDetalle();
                return;
            }
            CargarSobres();
        }

        private void CargarSobres()
        {
            _sobres = _presupuestoCtrl.ObtenerSobres(_periodo.Id);
            if (_sobreSeleccionadoId.HasValue && !_sobres.Any(s => s.GastoId == _sobreSeleccionadoId.Value))
                _sobreSeleccionadoId = null;
            if (!_sobreSeleccionadoId.HasValue && _sobres.Count > 0)
                _sobreSeleccionadoId = _sobres[0].GastoId;

            MostrarTarjetas();
            MostrarDetalle();
        }

        private PresupuestoResumen SobreSeleccionado =>
            _sobreSeleccionadoId.HasValue ? _sobres.FirstOrDefault(s => s.GastoId == _sobreSeleccionadoId.Value) : null;

        private void MostrarTarjetas()
        {
            for (int i = _panelTarjetas.Controls.Count - 1; i >= 0; i--) _panelTarjetas.Controls[i].Dispose();
            _panelTarjetas.Controls.Clear();
            _lblVacio.Visible = _sobres.Count == 0;
            if (_lblVacio.Visible) _lblVacio.BringToFront();
            foreach (var s in _sobres)
                _panelTarjetas.Controls.Add(CrearTarjeta(s));
        }

        private void MostrarDetalle()
        {
            var sobre = SobreSeleccionado;
            _dgvConsumos.Rows.Clear();

            if (sobre == null)
            {
                _lblDetalleTitulo.Text   = "Selecciona un sobre";
                _lblDetalleResumen.Text  = "";
                _btnNuevoConsumo.Enabled = false;
                return;
            }

            _lblDetalleTitulo.Text  = sobre.Titulo;
            _lblDetalleResumen.Text = $"${sobre.Gastado:N2} de ${sobre.Limite:N2}  -  " +
                (sobre.Disponible >= 0 ? $"te quedan ${sobre.Disponible:N2}" : $"excedido por ${-sobre.Disponible:N2}");
            _btnNuevoConsumo.Enabled = _periodo?.Estado == EstadoPeriodo.ABIERTO;

            foreach (var c in _presupuestoCtrl.ObtenerConsumos(sobre.GastoId))
            {
                int idx = _dgvConsumos.Rows.Add(c.Fecha.ToString("dd/MM/yy"), c.Descripcion, $"-${c.Monto:N2}", "...");
                _dgvConsumos.Rows[idx].Tag = c;
            }
            _dgvConsumos.ClearSelection();
        }

        private Panel CrearTarjeta(PresupuestoResumen r)
        {
            Color colorEstado =
                r.Estado == EstadoPresupuesto.EXCEDIDO ? C_EXCEDIDO :
                r.Estado == EstadoPresupuesto.CRITICO  ? C_CRITICO  :
                r.Estado == EstadoPresupuesto.ALERTA   ? C_ALERTA   : C_OK;

            bool  seleccionado = _sobreSeleccionadoId == r.GastoId;
            Color borde        = seleccionado ? colorEstado : C_BORDER;
            int   grosor       = seleccionado ? 2 : 1;

            var card = new Panel
            {
                Size = new Size(286, 128), Margin = new Padding(0, 0, 0, 12),
                BackColor = seleccionado ? C_SEL : C_SURFACE, Cursor = Cursors.Hand
            };
            card.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle,
                borde, grosor, ButtonBorderStyle.Solid, borde, grosor, ButtonBorderStyle.Solid,
                borde, grosor, ButtonBorderStyle.Solid, borde, grosor, ButtonBorderStyle.Solid);

            var lblTitulo = new Label
            {
                Text = r.Titulo, Location = new Point(14, 12), Size = new Size(258, 20),
                AutoEllipsis = true, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = C_TEXT
            };
            var lblCategoria = new Label
            {
                Text = r.CategoriaNombre, Location = new Point(14, 33), Size = new Size(258, 16),
                AutoEllipsis = true, Font = new Font("Segoe UI", 8F), ForeColor = C_MUTED
            };

            var barraFondo = new Panel { Location = new Point(14, 56), Size = new Size(258, 10), BackColor = C_BORDER };
            decimal fraccion = Math.Min(r.Porcentaje / 100m, 1m);
            barraFondo.Controls.Add(new Panel
            {
                Location = new Point(0, 0), Size = new Size((int)(258 * fraccion), 10), BackColor = colorEstado
            });

            var lblMonto = new Label
            {
                Text = $"${r.Gastado:N2} / ${r.Limite:N2}", Location = new Point(14, 74), AutoSize = true,
                Font = new Font("Segoe UI", 9.5F), ForeColor = C_MUTED
            };
            var lblPorcentaje = new Label
            {
                Text = $"{r.PorcentajeMostrado:N0}%", Location = new Point(14, 94), AutoSize = true,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold), ForeColor = colorEstado
            };
            var lblDisponible = new Label
            {
                Text = r.Disponible >= 0 ? $"quedan ${r.Disponible:N2}" : $"excedido ${-r.Disponible:N2}",
                Location = new Point(140, 102), Size = new Size(132, 18),
                TextAlign = ContentAlignment.MiddleRight, AutoEllipsis = true,
                Font = new Font("Segoe UI", 9F), ForeColor = C_MUTED
            };

            card.Controls.AddRange(new Control[] { lblTitulo, lblCategoria, barraFondo, lblMonto, lblPorcentaje, lblDisponible });

            EventHandler seleccionar = (s, e) =>
            {
                _sobreSeleccionadoId = r.GastoId;
                MostrarTarjetas();
                MostrarDetalle();
            };
            card.Click += seleccionar;
            foreach (Control hijo in card.Controls) hijo.Click += seleccionar;

            return card;
        }

        // ── Acciones ──────────────────────────────────────────────

        private void BtnNuevoConsumo_Click(object sender, EventArgs e)
        {
            var sobre = SobreSeleccionado;
            if (sobre == null) return;

            using (var form = new FormConsumoDialog(sobre.GastoId, sobre.Titulo, sobre.Disponible))
                if (form.ShowDialog() == DialogResult.OK)
                    EjecutarYRefrescar(() =>
                    {
                        _presupuestoCtrl.Guardar(form.Resultado, out string aviso);
                        return aviso;
                    });
        }

        private void DgvConsumos_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _dgvConsumos.Columns[e.ColumnIndex].Name != "Acc") return;
            if (_periodo?.Estado != EstadoPeriodo.ABIERTO) return;

            var c = (Consumo)_dgvConsumos.Rows[e.RowIndex].Tag;
            var menu = new ContextMenuStrip { Font = new Font("Segoe UI", 10F) };
            menu.Items.Add("Editar").Click   += (s, ev) => EditarConsumo(c);
            menu.Items.Add("Eliminar").Click += (s, ev) => EliminarConsumo(c);
            var rect = _dgvConsumos.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            menu.Show(_dgvConsumos, rect.Left, rect.Bottom);
        }

        private void EditarConsumo(Consumo c)
        {
            var sobre = SobreSeleccionado;
            if (sobre == null) return;

            // Al editar, lo que este consumo ya aporta vuelve a contar como disponible.
            decimal disponible = sobre.Disponible + c.Monto;
            using (var form = new FormConsumoDialog(sobre.GastoId, sobre.Titulo, disponible, c))
                if (form.ShowDialog() == DialogResult.OK)
                    EjecutarYRefrescar(() =>
                    {
                        _presupuestoCtrl.Actualizar(form.Resultado, out string aviso);
                        return aviso;
                    });
        }

        private void EliminarConsumo(Consumo c)
        {
            if (MessageBox.Show($"Eliminar el consumo \"{c.Descripcion}\" de ${c.Monto:N2}?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            EjecutarYRefrescar(() => { _presupuestoCtrl.Eliminar(c.Id); return null; });
        }

        // Corre la accion, refresca sobres y detalle, y muestra el aviso de umbral si lo hay.
        private void EjecutarYRefrescar(Func<string> accion)
        {
            try
            {
                string aviso = accion();
                CargarSobres();
                if (!string.IsNullOrEmpty(aviso))
                    MessageBox.Show(aviso, "Sobre", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string Capitalizar(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
    }
}
