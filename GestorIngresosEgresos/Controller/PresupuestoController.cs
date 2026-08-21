using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Repository;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GestorIngresosEgresos.Controller
{
    public class PresupuestoController
    {
        private readonly PresupuestoRepository _repo;
        private readonly CategoriaRepository _catRepo;

        public PresupuestoController()
        {
            _repo    = new PresupuestoRepository();
            _catRepo = new CategoriaRepository();
        }

        public List<PresupuestoResumen> ObtenerResumen(int periodoId) => _repo.ObtenerResumenPorPeriodo(periodoId);

        public List<CategoriaGasto> ObtenerCategoriasSinPresupuesto(int periodoId)
        {
            var asignadas = new HashSet<int>(_repo.ObtenerResumenPorPeriodo(periodoId).Select(r => r.CategoriaId));
            return _catRepo.ObtenerTodas().Where(c => !asignadas.Contains(c.Id)).ToList();
        }

        public Presupuesto Guardar(Presupuesto p)
        {
            if (p.Monto <= 0) throw new ArgumentException("El monto debe ser mayor a cero.");
            if (_repo.ObtenerPorCategoria(p.PeriodoId, p.CategoriaId) != null)
                throw new ArgumentException("Ya existe un presupuesto para esta categoria en este periodo. Editalo en su lugar.");
            return _repo.Guardar(p);
        }

        public void Actualizar(Presupuesto p)
        {
            if (p.Monto <= 0) throw new ArgumentException("El monto debe ser mayor a cero.");
            _repo.Actualizar(p);
        }

        public void Eliminar(int id) => _repo.Eliminar(id);
    }
}
