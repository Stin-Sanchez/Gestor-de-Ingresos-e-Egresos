using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Repository;
using System;
using System.Collections.Generic;

namespace GestorIngresosEgresos.Controller
{
    public class IngresoController
    {
        private readonly IngresoRepository _repo;

        public IngresoController()
        {
            _repo = new IngresoRepository();
        }

        public List<Ingreso> ObtenerPorPeriodo(int periodoId) => _repo.ObtenerPorPeriodo(periodoId);

        public Ingreso Guardar(Ingreso ing)
        {
            if (ing.Monto <= 0)
                throw new ArgumentException("El monto debe ser mayor a cero.");
            if (string.IsNullOrWhiteSpace(ing.Descripcion))
                throw new ArgumentException("La descripcion es obligatoria.");
            if (ing.Fecha == default) ing.Fecha = DateTime.Today;
            return _repo.Guardar(ing);
        }

        public void Actualizar(Ingreso ing)
        {
            if (ing.Monto <= 0) throw new ArgumentException("El monto debe ser mayor a cero.");
            _repo.Actualizar(ing);
        }

        public void Eliminar(int id) => _repo.Eliminar(id);
    }
}
