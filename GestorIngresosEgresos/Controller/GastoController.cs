using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Repository;
using System;
using System.Collections.Generic;

namespace GestorIngresosEgresos.Controller
{
    public class GastoController
    {
        private readonly GastoRepository _repo;
        private readonly CategoriaRepository _catRepo;
        private readonly PresupuestoRepository _presRepo;

        public GastoController()
        {
            _repo     = new GastoRepository();
            _catRepo  = new CategoriaRepository();
            _presRepo = new PresupuestoRepository();
        }

        public List<Gasto> ObtenerPorPeriodo(int periodoId)   => _repo.ObtenerPorPeriodo(periodoId);
        public List<Gasto> ObtenerAbonosPorDeuda(int deudaId) => _repo.ObtenerAbonosPorDeuda(deudaId);
        public List<CategoriaGasto> ObtenerCategorias()       => _catRepo.ObtenerTodas();

        public Gasto Guardar(Gasto g, out string avisoPresupuesto)
        {
            if (g.Monto <= 0)
                throw new ArgumentException("El monto debe ser mayor a cero.");
            if (string.IsNullOrWhiteSpace(g.Descripcion))
                throw new ArgumentException("La descripcion es obligatoria.");
            if (g.Fecha == default) g.Fecha = DateTime.Today;

            ValidarPresupuesto(g, excludeGastoId: null);

            _repo.Guardar(g);
            avisoPresupuesto = CalcularAviso(g);
            return g;
        }

        public void Actualizar(Gasto g, out string avisoPresupuesto)
        {
            if (g.Monto <= 0) throw new ArgumentException("El monto debe ser mayor a cero.");

            ValidarPresupuesto(g, excludeGastoId: g.Id);

            _repo.Actualizar(g);
            avisoPresupuesto = CalcularAviso(g);
        }

        public void Eliminar(int id) => _repo.Eliminar(id);

        private void ValidarPresupuesto(Gasto g, int? excludeGastoId)
        {
            if (!g.CategoriaId.HasValue) return;

            var presupuesto = _presRepo.ObtenerPorCategoria(g.PeriodoId, g.CategoriaId.Value);
            if (presupuesto == null) return;

            decimal gastadoActual = _presRepo.ObtenerGastado(g.PeriodoId, g.CategoriaId.Value, excludeGastoId);
            decimal disponible    = presupuesto.Monto - gastadoActual;
            if (g.Monto > disponible)
                throw new ArgumentException($"Este gasto supera tu presupuesto de {NombreCategoria(g.CategoriaId.Value)}. Disponible: ${disponible:N2}.");
        }

        private string CalcularAviso(Gasto g)
        {
            if (!g.CategoriaId.HasValue) return null;

            var presupuesto = _presRepo.ObtenerPorCategoria(g.PeriodoId, g.CategoriaId.Value);
            if (presupuesto == null || presupuesto.Monto <= 0) return null;

            decimal gastado     = _presRepo.ObtenerGastado(g.PeriodoId, g.CategoriaId.Value, null);
            decimal porcentaje  = Math.Round(gastado / presupuesto.Monto * 100m, 0);
            decimal disponible  = presupuesto.Monto - gastado;
            string  categoria   = NombreCategoria(g.CategoriaId.Value);

            if (porcentaje >= 100)
                return $"Has agotado tu presupuesto de {categoria} este mes.";
            if (porcentaje >= 50)
                return $"Has consumido el {porcentaje:N0}% de tu presupuesto de {categoria}. Te quedan ${disponible:N2}.";
            return null;
        }

        private string NombreCategoria(int categoriaId) =>
            _catRepo.ObtenerTodas().Find(c => c.Id == categoriaId)?.Nombre ?? "esta categoria";
    }
}
