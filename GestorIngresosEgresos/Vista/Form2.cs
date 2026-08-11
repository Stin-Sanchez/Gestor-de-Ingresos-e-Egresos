using GestorIngresosEgresos.Controller;
using GestorIngresosEgresos.Modelo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GestorIngresosEgresos.Vista
{
    public partial class Form2 : Form
    {

        private MovimientoController controller;
        private bool isDarkMode = false;
        // Controles UI
        private Panel panelHeader;
        private Panel panelTiles;
        private Panel tileBalance;
        private Panel tileGastos;
        private Panel tileIngresos;
        private Label  lblBalanceMonto;
        private Label  lblGastosMonto;
        private Label  lblIngresosMonto;
        private DataGridView dgvMovimientos;
        private Button btnNuevo;
        private Button btnRefrescar;
        private CheckBox chkDarkMode;
        private TextBox txtBuscar;
        private ComboBox cboFiltroTipo;
        private ComboBox cboMes;        // <-- Agregar esta
        private NumericUpDown numAnio;  // <-- Agregar esta
        private Panel panelContenedor;
        private Panel panelControles;

        public Form2()
        {
            controller = new MovimientoController();
            InitializeComponent();
            initComponent();
            CargarMovimientos();
            ActualizarTotales();
            dgvMovimientos.ClearSelection();
        }


        private void initComponent()
        {
            // Configuración del formulario principal
            this.Text = "Gestor de Ingresos y Egresos";
            this.Size = new Size(1300, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F);

            // Panel Header
            panelHeader = new Panel
            {
                Height = 70,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(91, 95, 237)
            };

            Label lblTitulo = new Label
            {
                Text = "💰 GESTOR DE INGRESOS Y EGRESOS",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 20)
            };

            // CheckBox Dark Mode
            chkDarkMode = new CheckBox
            {
                Text = "🌙 Dark Mode",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point( 50, 20),
                AutoSize = false, 
                Size = new Size(140, 35), 
                Appearance = Appearance.Button,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(91, 95, 237),
                TextAlign = ContentAlignment.MiddleCenter, // Centrar el texto
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            chkDarkMode.FlatAppearance.BorderSize = 0;
            chkDarkMode.CheckedChanged += ChkDarkMode_CheckedChanged;

            panelHeader.Controls.AddRange(new Control[] { lblTitulo, chkDarkMode });

            // Panel Contenedor Principal
            panelContenedor = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(20)
            };

            // Panel de Tiles
            panelTiles = new Panel
            {
                Height = 130,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent
            };

            tileBalance = CrearTile("BALANCE TOTAL", "$0.00", 0);
            tileGastos = CrearTile("GASTOS TOTALES", "$0.00", 420);
            tileIngresos = CrearTile("INGRESOS TOTALES", "$0.00", 840);

            lblBalanceMonto = (Label)tileBalance.Controls[1];
            lblGastosMonto = (Label)tileGastos.Controls[1];
            lblIngresosMonto = (Label)tileIngresos.Controls[1];

            panelTiles.Controls.AddRange(new Control[] { tileBalance, tileGastos, tileIngresos });

            // Panel de Controles
            panelControles = new Panel
            {
                Height = 70,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 10, 0, 10)
            };

            Label lblBuscar = new Label
            {
                Text = "🔍 Buscar:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(0, 5),
                AutoSize = true
            };

            txtBuscar = new TextBox
            {
                Location = new Point(0, 28),
                Size = new Size(300, 30),
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            txtBuscar.TextChanged += TxtBuscar_TextChanged;

            Label lblFiltro = new Label
            {
                Text = "🔖 Filtrar por tipo:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(320, 5),
                AutoSize = true
            };

            cboFiltroTipo = new ComboBox
            {
                Location = new Point(320, 28),
                Size = new Size(200, 30),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            cboFiltroTipo.Items.AddRange(new object[] { "Todos", "Ingreso", "Egreso" });
            cboFiltroTipo.SelectedIndex = 0;
            cboFiltroTipo.SelectedIndexChanged += CboFiltroTipo_SelectedIndexChanged;

            Label lblPeriodo = new Label
            {
                Text = "🗓️ Periodo:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(540, 5),
                AutoSize = true
            };

            cboMes = new ComboBox
            {
                Location = new Point(540, 28),
                Size = new Size(130, 30),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            cboMes.Items.AddRange(new object[] {
                "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
                "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
            });
            cboMes.SelectedIndex = DateTime.Now.Month - 1; // Mes actual por defecto
            cboMes.SelectedIndexChanged += CboPeriodo_SelectedIndexChanged;

            numAnio = new NumericUpDown
            {
                Location = new Point(680, 28),
                Size = new Size(90, 30),
                Font = new Font("Segoe UI", 10F),
                Minimum = 2000,
                Maximum = 2100,
                Value = DateTime.Now.Year
            };
            numAnio.ValueChanged += CboPeriodo_SelectedIndexChanged;

            btnRefrescar = new Button
            {
                Text = "🔄 Refrescar",
                Location = new Point(790, 22),
                Size = new Size(150, 30),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(100, 116, 139),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRefrescar.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnRefrescar.FlatAppearance.BorderSize = 1;
            btnRefrescar.Click += (s, e) => { CargarMovimientos(); ActualizarTotales(); };

            btnNuevo = new Button
            {
                Text = "+ NUEVO MOVIMIENTO",
                Location = new Point(1050, 22),
                Size = new Size(210, 30),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                BackColor = Color.FromArgb(91, 95, 237),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnNuevo.FlatAppearance.BorderSize = 0;
            btnNuevo.Click += BtnNuevo_Click;

            panelControles.Controls.AddRange(new Control[] {
                lblBuscar, txtBuscar, lblFiltro, cboFiltroTipo,
                lblPeriodo, cboMes, numAnio,
                btnRefrescar, btnDeudas, btnNuevo
            });

            // Panel Tabla
            Panel panelTabla = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(15)
            };

            // Borde sutil para el panel
            panelTabla.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, panelTabla.ClientRectangle,
                    Color.FromArgb(226, 232, 240), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(226, 232, 240), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(226, 232, 240), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(226, 232, 240), 1, ButtonBorderStyle.Solid);
            };

            Label lblTituloTabla = new Label
            {
                Text = "📋 Historial de Movimientos",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Height = 45,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0)
            };

            // DataGridView
            dgvMovimientos = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 10F),
                ColumnHeadersHeight = 45,
                RowTemplate = { Height = 50 },
                EnableHeadersVisualStyles = false,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(226, 232, 240)
            };

            // Estilo del header
            dgvMovimientos.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            dgvMovimientos.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(100, 116, 139);
            dgvMovimientos.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgvMovimientos.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvMovimientos.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 10, 0);

            // Estilo de las celdas
            dgvMovimientos.DefaultCellStyle.SelectionBackColor = Color.FromArgb(245, 245, 245);
            dgvMovimientos.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 41, 59);
            dgvMovimientos.DefaultCellStyle.Padding = new Padding(10, 8, 10, 8);
            dgvMovimientos.DefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
         

            ConfigurarColumnas();

            panelTabla.Controls.Add(dgvMovimientos);
            panelTabla.Controls.Add(lblTituloTabla);

            panelContenedor.Controls.Add(panelTabla);
            panelContenedor.Controls.Add(panelControles);
            panelContenedor.Controls.Add(panelTiles);

            this.Controls.Add(panelContenedor);
            this.Controls.Add(panelHeader);
        }

        private Panel CrearTile(string titulo, string valor, int x)
        {
            Panel tile = new Panel
            {
                Size = new Size(400, 110),
                Location = new Point(x, 10),
                BackColor = Color.White
            };

            // Borde sutil
            tile.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, tile.ClientRectangle,
                    Color.FromArgb(226, 232, 240), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(226, 232, 240), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(226, 232, 240), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(226, 232, 240), 1, ButtonBorderStyle.Solid);
            };

            Label lblTitulo = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(20, 20),
                AutoSize = true
            };

            Label lblValor = new Label
            {
                Text = valor,
                Font = new Font("Segoe UI", 28F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(20, 50),
                AutoSize = true
            };

            tile.Controls.Add(lblTitulo);
            tile.Controls.Add(lblValor);

            return tile;
        }

        private void ConfigurarColumnas()
        {
            dgvMovimientos.Columns.Clear();

            DataGridViewTextBoxColumn colFecha = new DataGridViewTextBoxColumn
            {
                Name = "Fecha",
                HeaderText = "FECHA",
                Width = 120,
                DefaultCellStyle = {
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    ForeColor = Color.FromArgb(100, 116, 139)
                }
            };

            DataGridViewTextBoxColumn colTipo = new DataGridViewTextBoxColumn
            {
                Name = "Tipo",
                HeaderText = "TIPO",
                Width = 130,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
            };

            DataGridViewTextBoxColumn colNombre = new DataGridViewTextBoxColumn
            {
                Name = "Nombre",
                HeaderText = "DESCRIPCIÓN",
                FillWeight = 150,
                DefaultCellStyle = {
                    Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                    ForeColor = Color.FromArgb(30, 41, 59)
                }
            };

            DataGridViewTextBoxColumn colMonto = new DataGridViewTextBoxColumn
            {
                Name = "Monto",
                HeaderText = "MONTO",
                Width = 140,
                DefaultCellStyle = {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold)
                }
            };

            DataGridViewTextBoxColumn colDescripcion = new DataGridViewTextBoxColumn
            {
                Name = "Descripcion",
                HeaderText = "DETALLE",
                FillWeight = 200,
                DefaultCellStyle = {
                    ForeColor = Color.FromArgb(100, 116, 139)
                }
            };

            // Columna de acciones con tres puntos
            DataGridViewTextBoxColumn colAcciones = new DataGridViewTextBoxColumn
            {
                Name = "Acciones",
                HeaderText = "ACCIONES",
                Width = 60,
                DefaultCellStyle = {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 16F),
                    ForeColor = Color.FromArgb(148, 163, 184)
                }
            };

            dgvMovimientos.Columns.AddRange(new DataGridViewColumn[] {
                colFecha, colTipo, colNombre, colMonto, colDescripcion, colAcciones
            });

            dgvMovimientos.CellClick += DgvMovimientos_CellClick;
            dgvMovimientos.CellFormatting += DgvMovimientos_CellFormatting;
            dgvMovimientos.CellMouseEnter += DgvMovimientos_CellMouseEnter;
            dgvMovimientos.CellMouseLeave += DgvMovimientos_CellMouseLeave;
        }

        private void DgvMovimientos_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvMovimientos.Columns[e.ColumnIndex].Name == "Acciones")
            {
                dgvMovimientos.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.ForeColor = Color.FromArgb(91, 95, 237);
                dgvMovimientos.Cursor = Cursors.Hand;
            }
        }

        private void DgvMovimientos_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvMovimientos.Columns[e.ColumnIndex].Name == "Acciones")
            {
                dgvMovimientos.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.ForeColor = Color.FromArgb(148, 163, 184);
                dgvMovimientos.Cursor = Cursors.Default;
            }
        }

        private void CargarMovimientos()
        {
            try
            {
                List<Movimiento> movimientos = controller.ObtenerTodosLosMovimientos();
                MostrarMovimientos(movimientos);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar movimientos: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MostrarMovimientos(List<Movimiento> movimientos)
        {
            dgvMovimientos.Rows.Clear();

            foreach (var mov in movimientos.OrderByDescending(m => m.Fecha))
            {
                string tipoIcono = mov.Tipo == TipoMovimiento.INGRESO ? "↑ Ingreso" : "↓ Egreso";
                string montoTexto = mov.Monto >= 0 ? $"+${mov.Monto:N2}" : $"-${Math.Abs(mov.Monto):N2}";

                int rowIndex = dgvMovimientos.Rows.Add(
                    mov.Fecha.ToString("dd/MM/yyyy"),
                    tipoIcono,
                    mov.Nombre,
                    montoTexto,
                    mov.Descripcion ?? "Sin descripción",
                    "⋮" // Tres puntos verticales
                );

                dgvMovimientos.Rows[rowIndex].Tag = mov.Id;
            }
        }

        private void DgvMovimientos_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // Formatear columna Monto
            if (dgvMovimientos.Columns[e.ColumnIndex].Name == "Monto")
            {
                string valor = e.Value?.ToString() ?? "";
                if (valor.startsWith("+"))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(16, 185, 129); // Verde moderno
                }
                else if (valor.StartsWith("-"))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(239, 68, 68); // Rojo moderno
                }
            }

            // Formatear columna Tipo - SIN fondos de colores, solo texto
            if (dgvMovimientos.Columns[e.ColumnIndex].Name == "Tipo")
            {
                string valor = e.Value?.ToString() ?? "";
                if (valor.Contains("Ingreso"))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(16, 185, 129);
                    e.CellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                }
                else if (valor.Contains("Egreso"))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(239, 68, 68);
                    e.CellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                }
            }
        }

        private void ActualizarTotales()
        {
            try
            {
                var resumen = controller.ObtenerResumenPorMes((int)numAnio.Value, cboMes.SelectedIndex + 1);

                lblBalanceMonto.Text = $"${resumen.BalanceTotal:N2}";
                lblGastosMonto.Text = $"${resumen.TotalGastos:N2}";
                lblIngresosMonto.Text = $"${resumen.TotalIngresos:N2}";

                if (resumen.BalanceTotal >= 0)
                {
                    lblBalanceMonto.ForeColor = Color.FromArgb(16, 185, 129);
                }
                else
                {
                    lblBalanceMonto.ForeColor = Color.FromArgb(239, 68, 68);
                }

                lblGastosMonto.ForeColor = Color.FromArgb(239, 68, 68);
                lblIngresosMonto.ForeColor = Color.FromArgb(16, 185, 129);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al calcular totales: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvMovimientos_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (dgvMovimientos.Columns[e.ColumnIndex].Name == "Acciones")
            {
                int movId = (int)dgvMovimientos.Rows[e.RowIndex].Tag;
                MostrarMenuAcciones(movId);
            }
        }

        private void MostrarMenuAcciones(int movId)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.BackColor = Color.White;
            menu.Font = new Font("Segoe UI", 10F);
            menu.RenderMode = ToolStripRenderMode.Professional;

            ToolStripMenuItem itemDetalle = new ToolStripMenuItem("👁️ Ver Detalle");
            itemDetalle.ForeColor = Color.FromArgb(30, 41, 59);
            itemDetalle.Click += (s, e) => VerDetalle(movId);

            ToolStripMenuItem itemEditar = new ToolStripMenuItem("✏️ Editar");
            itemEditar.ForeColor = Color.FromArgb(30, 41, 59);
            itemEditar.Click += (s, e) => EditarMovimiento(movId);

            ToolStripMenuItem itemEliminar = new ToolStripMenuItem("🗑️ Eliminar");
            itemEliminar.ForeColor = Color.FromArgb(239, 68, 68);
            itemEliminar.Click += (s, e) => EliminarMovimiento(movId);

            menu.Items.AddRange(new ToolStripItem[] {
                itemDetalle, itemEditar, new ToolStripSeparator(), itemEliminar
            });

            menu.Show(Cursor.Position);
        }

        private void VerDetalle(int movId)
        {
            try
            {
                Movimiento mov = controller.ObtenerMovimientoPorId(movId);
         

                if (mov != null)
                {
                    string detalle = $"📅 Fecha: {mov.Fecha:dd/MM/yyyy}\n\n" +
                                    $"📝 Nombre: {mov.Nombre}\n\n" +
                                    $"🔖 Tipo: {mov.Tipo}\n\n" +
                                    $"💵 Monto: ${Math.Abs(mov.Monto):N2}\n\n" +
                                    $"📄 Descripción:\n{mov.Descripcion}";

                    MessageBox.Show(detalle, "Detalle del Movimiento",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EditarMovimiento(int movId)
        {
            try
            {
                var mov = controller.ObtenerMovimientoPorId(movId);
                

                if (mov != null)
                {
                    using (FormMovimiento form = new FormMovimiento(mov))
                    {
                        if (form.ShowDialog() == DialogResult.OK)
                        {
                            controller.ActualizarMovimiento(form.MovimientoEditado);
                            CargarMovimientos();
                            ActualizarTotales();
                            MessageBox.Show("✅ Movimiento actualizado exitosamente", "Éxito",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al editar: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EliminarMovimiento(int movId)
        {
            try
            {
                var result = MessageBox.Show(
                    "⚠️ ¿Está seguro de eliminar este movimiento?\n\nEsta acción no se puede deshacer.",
                    "Confirmar Eliminación",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    controller.EliminarMovimiento(movId);
                    CargarMovimientos();
                    ActualizarTotales();
                    MessageBox.Show("✅ Movimiento eliminado exitosamente", "Éxito",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al eliminar: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnNuevo_Click(object sender, EventArgs e)
        {
            try
            {
                using (FormMovimiento form = new FormMovimiento(null))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        controller.GuardarMovimiento(form.MovimientoEditado);
                        CargarMovimientos();
                        ActualizarTotales();
                        MessageBox.Show("✅ Movimiento guardado exitosamente", "Éxito",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TxtBuscar_TextChanged(object sender, EventArgs e)
        {
            AplicarFiltros();
        }

        private void CboFiltroTipo_SelectedIndexChanged(object sender, EventArgs e)
        {
            AplicarFiltros();
        }

        private void CboPeriodo_SelectedIndexChanged(object sender, EventArgs e)
        {
            AplicarFiltros();
            ActualizarTotales();
        }

        private void AplicarFiltros()
        {
            try
            {
                // Filtro base: periodo mensual seleccionado
                int anio = (int)numAnio.Value;
                int mes = cboMes.SelectedIndex + 1;
                var movimientos = controller.ObtenerMovimientosPorMes(anio, mes);

                // Filtro por tipo
                if (cboFiltroTipo.SelectedIndex > 0)
                {
                    string tipoSeleccionado = cboFiltroTipo.SelectedItem.ToString();
                    if (tipoSeleccionado == "Ingreso")
                    {
                        movimientos = movimientos.Where(m => m.Tipo == TipoMovimiento.INGRESO).ToList();
                    }
                    else if (tipoSeleccionado == "Egreso")
                    {
                        movimientos = movimientos.Where(m => m.Tipo == TipoMovimiento.EGRESO).ToList();
                    }
                }

                // Filtro por búsqueda
                string busqueda = txtBuscar.Text.ToLower().Trim();
                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    movimientos = movimientos.Where(m =>
                        m.Nombre.ToLower().Contains(busqueda) ||
                        (m.Descripcion?.ToLower().Contains(busqueda) ?? false)
                    ).ToList();
                }

                MostrarMovimientos(movimientos);
            }
            catch { }
        }

        private void ChkDarkMode_CheckedChanged(object sender, EventArgs e)
        {
            if (chkDarkMode.Checked)
            {
                AplicarDarkMode();
                chkDarkMode.BackColor = Color.FromArgb(245, 158, 11);
                chkDarkMode.Text = "☀️ Light Mode";
            }
            else
            {
                AplicarLightMode();
                chkDarkMode.BackColor = Color.FromArgb(91, 95, 237);
                chkDarkMode.Text = "🌙 Dark Mode";
            }
        }

        private void AplicarDarkMode()
        {
            isDarkMode = true;
            Color fondoOscuro = Color.FromArgb(30, 30, 30);
            Color fondoCards = Color.FromArgb(45, 45, 48); 
            Color textoBlanco = Color.White;
            Color textoGris = Color.FromArgb(200, 200, 200);

            // 1. Fondos Generales
            this.BackColor = fondoOscuro;
            panelContenedor.BackColor = fondoOscuro;
            panelHeader.BackColor = Color.FromArgb(25, 25, 25); 
            panelTiles.BackColor = fondoOscuro;
            panelControles.BackColor = fondoOscuro;

          
            foreach (Control ctrl in panelTiles.Controls)
            {
                if (ctrl is Panel card)
                {
                    card.BackColor = fondoCards; 

                 
                    foreach (Control child in card.Controls)
                    {
                        if (child is Label lbl)
                        {
                          
                            if (lbl.Font.Size < 20)
                            {
                                lbl.ForeColor = textoGris;
                            }
                        }
                    }
                }
            }

          
            txtBuscar.BackColor = fondoCards;
            txtBuscar.ForeColor = textoBlanco;
            cboFiltroTipo.BackColor = fondoCards;
            cboFiltroTipo.ForeColor = textoBlanco;

           
            foreach (Control ctrl in panelControles.Controls)
            {
                if (ctrl is Label lbl) lbl.ForeColor = textoGris;
            }

          
            btnRefrescar.BackColor = fondoCards;
            btnRefrescar.ForeColor = textoBlanco;
            btnRefrescar.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);

         
            dgvMovimientos.BackgroundColor = fondoOscuro;
            dgvMovimientos.DefaultCellStyle.BackColor = fondoCards;
            dgvMovimientos.DefaultCellStyle.ForeColor = textoBlanco;
            dgvMovimientos.GridColor = Color.FromArgb(60, 60, 60);

           
            dgvMovimientos.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(20, 20, 20);
            dgvMovimientos.ColumnHeadersDefaultCellStyle.ForeColor = textoBlanco;
            dgvMovimientos.EnableHeadersVisualStyles = false; 

          
          
            if (dgvMovimientos.Columns["Nombre"] != null)
                dgvMovimientos.Columns["Nombre"].DefaultCellStyle.ForeColor = textoBlanco;

            if (dgvMovimientos.Columns["Descripcion"] != null)
                dgvMovimientos.Columns["Descripcion"].DefaultCellStyle.ForeColor = textoGris;

         
            foreach (Control ctrl in panelContenedor.Controls)
            {
              
                if (ctrl is Panel pnl && pnl != panelTiles && pnl != panelControles)
                {
                    pnl.BackColor = fondoCards;
                    foreach (Control child in pnl.Controls)
                    {
                        if (child is Label lbl) lbl.ForeColor = textoBlanco; 
                    }
                }
            }
        }

        private void AplicarLightMode()
        {
            isDarkMode = false;
            Color fondoClaro = Color.FromArgb(248, 250, 252);
            Color colorTextoOscuro = Color.FromArgb(30, 41, 59);
            Color colorTextoGris = Color.FromArgb(100, 116, 139);

         
            this.BackColor = fondoClaro;
            panelContenedor.BackColor = fondoClaro;
            panelHeader.BackColor = Color.FromArgb(91, 95, 237);
            panelTiles.BackColor = Color.Transparent;
            panelControles.BackColor = Color.Transparent;

           
            foreach (Control ctrl in panelTiles.Controls)
            {
                if (ctrl is Panel card)
                {
                    card.BackColor = Color.White;
                    foreach (Control child in card.Controls)
                    {
                        if (child is Label lbl && lbl.Font.Size < 20)
                        {
                            lbl.ForeColor = Color.FromArgb(148, 163, 184); 
                        }
                    }
                }
            }

          
            txtBuscar.BackColor = Color.White;
            txtBuscar.ForeColor = colorTextoOscuro;
            cboFiltroTipo.BackColor = Color.White;
            cboFiltroTipo.ForeColor = colorTextoOscuro;

          
            btnRefrescar.BackColor = Color.White;
            btnRefrescar.ForeColor = colorTextoGris;
            btnRefrescar.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);

         
            foreach (Control ctrl in panelControles.Controls)
            {
                if (ctrl is Label lbl) lbl.ForeColor = colorTextoGris;
            }

          
            dgvMovimientos.BackgroundColor = Color.White;
            dgvMovimientos.DefaultCellStyle.BackColor = Color.White;
            dgvMovimientos.DefaultCellStyle.ForeColor = colorTextoOscuro;
            dgvMovimientos.GridColor = Color.FromArgb(226, 232, 240);

            dgvMovimientos.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgvMovimientos.ColumnHeadersDefaultCellStyle.ForeColor = colorTextoGris;

         
            if (dgvMovimientos.Columns["Nombre"] != null)
                dgvMovimientos.Columns["Nombre"].DefaultCellStyle.ForeColor = colorTextoOscuro;

            if (dgvMovimientos.Columns["Descripcion"] != null)
                dgvMovimientos.Columns["Descripcion"].DefaultCellStyle.ForeColor = colorTextoGris;

          
            foreach (Control ctrl in panelContenedor.Controls)
            {
                if (ctrl is Panel pnl && pnl != panelTiles && pnl != panelControles)
                {
                    pnl.BackColor = Color.White;
                    foreach (Control child in pnl.Controls)
                    {
                        if (child is Label lbl) lbl.ForeColor = colorTextoOscuro;
                    }
                }
            }

          
            chkDarkMode.BackColor = Color.FromArgb(91, 95, 237);
            chkDarkMode.Text = "🌙 Dark Mode";
        }
    

    private void Form2_Load(object sender, EventArgs e)
        {

        }

        private void Form2_Load_1(object sender, EventArgs e)
        {

        }
    }
}
