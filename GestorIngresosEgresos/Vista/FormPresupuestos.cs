using GestorIngresosEgresos.Controller;
using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GestorIngresosEgresos.Vista
{
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
        static readonly Color C_ACCENT   = Color.FromArgb(37, 99, 235);

        private readonly PeriodoController     _periodoCtrl     = new PeriodoController();
        private readonly PresupuestoController _presupuestoCtrl = new PresupuestoController();

        private Periodo _periodo;
        private Label   _lblNombre, _lblEstado, _lblVacio;
        private Button  _btnAnterior, _btnSiguiente, _btnNuevo;
        private FlowLayoutPanel _panelTarjetas;

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

            var barPanel = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = C_SURFACE, Padding = new Padding(14, 10, 14, 10) };
            _btnNuevo = new Button
            {
                Text = "+ Presupuesto", Size = new Size(130, 30), Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = C_ACCENT, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            _btnNuevo.FlatAppearance.BorderSize = 0;
            _btnNuevo.Click += BtnNuevo_Click;
            var flp = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = C_SURFACE, WrapContents = false };
            flp.Controls.Add(_btnNuevo);
            barPanel.Controls.Add(flp);

            var contentPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14), BackColor = C_BG };
            _panelTarjetas = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = C_BG };
            _lblVacio = new Label
            {
                Text = "No hay presupuestos asignados este mes. Usa \"+ Presupuesto\" para separar un monto por categoria.",
                AutoSize = true, ForeColor = C_MUTED, Font = new Font("Segoe UI", 10F), Location = new Point(4, 4), Visible = false
            };
            contentPanel.Controls.Add(_lblVacio);
            contentPanel.Controls.Add(_panelTarjetas);

            this.Controls.Add(contentPanel);
            this.Controls.Add(barPanel);
            this.Controls.Add(header);
        }

        private Button NavBtn(string t) => new Button
        {
            Text = t, Size = new Size(30, 30), Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 163, 184), BackColor = C_SIDEBAR, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };

        private void CargarPeriodo()
        {
            _periodo = _periodoCtrl.ObtenerOCrearPeriodo(PeriodoManager.Anio, PeriodoManager.Mes);

            _lblNombre.Text = _periodo?.Nombre ?? Capitalizar(PeriodoManager.NombrePeriodo);
            bool sinDatos = _periodo == null;
            bool abierto  = _periodo?.Estado == EstadoPeriodo.ABIERTO;

            _lblEstado.Text      = sinDatos ? "SIN DATOS" : (abierto ? "ABIERTO" : "CERRADO");
            _lblEstado.BackColor = sinDatos ? Color.FromArgb(51, 65, 85) : (abierto ? Color.FromArgb(6, 78, 59) : Color.FromArgb(69, 26, 3));
            _lblEstado.ForeColor = sinDatos ? C_MUTED : (abierto ? C_OK : Color.FromArgb(245, 158, 11));
            _btnNuevo.Enabled    = abierto;

            if (sinDatos) { MostrarTarjetas(new List<PresupuestoResumen>()); return; }
            CargarTarjetas();
        }

        private void CargarTarjetas() => MostrarTarjetas(_presupuestoCtrl.ObtenerResumen(_periodo.Id));

        private void MostrarTarjetas(List<PresupuestoResumen> resumen)
        {
            for (int i = _panelTarjetas.Controls.Count - 1; i >= 0; i--) _panelTarjetas.Controls[i].Dispose();
            _panelTarjetas.Controls.Clear();
            _lblVacio.Visible = resumen.Count == 0;
            if (_lblVacio.Visible) _lblVacio.BringToFront();
            foreach (var r in resumen)
                _panelTarjetas.Controls.Add(CrearTarjeta(r));
        }

        private Panel CrearTarjeta(PresupuestoResumen r)
        {
            Color colorEstado =
                r.Estado == EstadoPresupuesto.EXCEDIDO ? C_EXCEDIDO :
                r.Estado == EstadoPresupuesto.CRITICO  ? C_CRITICO  :
                r.Estado == EstadoPresupuesto.ALERTA   ? C_ALERTA   : C_OK;

            var card = new Panel { Size = new Size(260, 150), Margin = new Padding(0, 0, 14, 14), BackColor = C_SURFACE };
            card.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle,
                C_BORDER, 1, ButtonBorderStyle.Solid, C_BORDER, 1, ButtonBorderStyle.Solid,
                C_BORDER, 1, ButtonBorderStyle.Solid, C_BORDER, 1, ButtonBorderStyle.Solid);

            var lblNombre = new Label
            {
                Text = r.CategoriaNombre, Location = new Point(14, 12), AutoSize = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = C_TEXT
            };

            var barraFondo = new Panel { Location = new Point(14, 42), Size = new Size(232, 10), BackColor = C_BORDER };
            decimal fraccion = Math.Min(r.Porcentaje / 100m, 1m);
            var barraRelleno = new Panel
            {
                Location  = new Point(0, 0),
                Size      = new Size((int)(232 * fraccion), 10),
                BackColor = colorEstado
            };
            barraFondo.Controls.Add(barraRelleno);

            var lblMonto = new Label
            {
                Text = $"${r.Gastado:N2} / ${r.Limite:N2}", Location = new Point(14, 60), AutoSize = true,
                Font = new Font("Segoe UI", 9.5F), ForeColor = C_MUTED
            };
            var lblPorcentaje = new Label
            {
                Text = $"{r.Porcentaje:N0}%", Location = new Point(14, 82), AutoSize = true,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = colorEstado
            };
            var lblDisponible = new Label
            {
                Text = r.Disponible >= 0 ? $"Disponible: ${r.Disponible:N2}" : $"Excedido por: ${-r.Disponible:N2}",
                Location = new Point(14, 112), AutoSize = true,
                Font = new Font("Segoe UI", 9F), ForeColor = C_MUTED
            };

            var btnEditar = new Button
            {
                Text = "Editar", Size = new Size(60, 24), Location = new Point(122, 112),
                Font = new Font("Segoe UI", 8F), ForeColor = C_MUTED, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnEditar.FlatAppearance.BorderSize = 0;
            btnEditar.Click += (s, e) => EditarPresupuesto(r);

            var btnEliminar = new Button
            {
                Text = "Eliminar", Size = new Size(70, 24), Location = new Point(182, 112),
                Font = new Font("Segoe UI", 8F), ForeColor = C_EXCEDIDO, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnEliminar.FlatAppearance.BorderSize = 0;
            btnEliminar.Click += (s, e) => EliminarPresupuesto(r);

            if (_periodo.Estado != EstadoPeriodo.ABIERTO) { btnEditar.Enabled = false; btnEliminar.Enabled = false; }

            card.Controls.AddRange(new Control[] { lblNombre, barraFondo, lblMonto, lblPorcentaje, lblDisponible, btnEditar, btnEliminar });
            return card;
        }

        private void BtnNuevo_Click(object sender, EventArgs e)
        {
            var disponibles = _presupuestoCtrl.ObtenerCategoriasSinPresupuesto(_periodo.Id);
            if (disponibles.Count == 0)
            {
                MessageBox.Show("Ya asignaste un presupuesto a todas las categorias este mes.", "Presupuestos",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var form = new FormPresupuestoDialog(_periodo.Id, disponibles))
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try { _presupuestoCtrl.Guardar(form.Resultado); CargarTarjetas(); }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
        }

        private void EditarPresupuesto(PresupuestoResumen r)
        {
            using (var form = new FormPresupuestoDialog(_periodo.Id, r))
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try { _presupuestoCtrl.Actualizar(form.Resultado); CargarTarjetas(); }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
        }

        private void EliminarPresupuesto(PresupuestoResumen r)
        {
            if (MessageBox.Show($"Eliminar el presupuesto de \"{r.CategoriaNombre}\"?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try { _presupuestoCtrl.Eliminar(r.Id); CargarTarjetas(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private static string Capitalizar(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
    }
}
