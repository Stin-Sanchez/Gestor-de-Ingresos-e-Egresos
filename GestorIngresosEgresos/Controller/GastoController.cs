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

        public GastoController()
        {
            _repo    = new GastoRepository();
            _catRepo = new CategoriaRepository();
        }

        public List<Gasto> ObtenerPorPeriodo(int periodoId)   => _repo.ObtenerPorPeriodo(periodoId);
        public List<Gasto> ObtenerAbonosPorDeuda(int deudaId) => _repo.ObtenerAbonosPorDeuda(deudaId);
        public List<CategoriaGasto> ObtenerCategorias()       => _catRepo.ObtenerTodas();

        public Gasto Guardar(Gasto g)
        {
            if (g.Monto <= 0)
                throw new ArgumentException("El monto debe ser mayor a cero.");
            if (string.IsNullOrWhiteSpace(g.Descripcion))
                throw new ArgumentException("La descripcion es obligatoria.");
            if (g.Fecha == default) g.Fecha = DateTime.Today;
            return _repo.Guardar(g);
        }

        public void Actualizar(Gasto g)
        {
            if (g.Monto <= 0) throw new ArgumentException("El monto debe ser mayor a cero.");
            _repo.Actualizar(g);
        }

        public void Eliminar(int id) => _repo.Eliminar(id);
    }
}
