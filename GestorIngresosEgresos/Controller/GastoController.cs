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
            if (string.IsNullOrWhiteSpace(g.Descripcion))
                throw new ArgumentException("La descripcion es obligatoria.");

            ValidarContraConsumos(g);
            _repo.Actualizar(g);
        }

        public void Eliminar(int id) => _repo.Eliminar(id);

        // Un sobre no puede quedar por debajo de lo que ya se consumio de el, ni dejar de
        // ser sobre mientras tenga consumos: en ambos casos el registro quedaria incoherente.
        private void ValidarContraConsumos(Gasto g)
        {
            decimal consumido = _presRepo.ObtenerConsumido(g.Id, null);
            if (consumido <= 0) return;

            if (!g.EsSobre)
                throw new ArgumentException(
                    $"Este egreso ya tiene ${consumido:N2} en consumos registrados. Elimina esos consumos antes de dejar de tratarlo como sobre.");

            if (g.Monto < consumido)
                throw new ArgumentException(
                    $"Ya consumiste ${consumido:N2} de este sobre, asi que no puedes bajarlo a ${g.Monto:N2}.");
        }
    }
}
