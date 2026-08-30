using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Repository;
using System;
using System.Collections.Generic;

namespace GestorIngresosEgresos.Controller
{
    // Coordina los sobres y sus consumos. Un consumo no puede pasarse del sobre:
    // se bloquea, igual que se decidio para los gastos contra su presupuesto.
    public class PresupuestoController
    {
        private readonly PresupuestoRepository _repo;

        public PresupuestoController()
        {
            _repo = new PresupuestoRepository();
        }

        public List<PresupuestoResumen> ObtenerSobres(int periodoId) => _repo.ObtenerSobresPorPeriodo(periodoId);
        public PresupuestoResumen ObtenerResumen(int gastoId)        => _repo.ObtenerResumenPorGasto(gastoId);
        public List<Consumo> ObtenerConsumos(int gastoId)            => _repo.ObtenerConsumos(gastoId);

        public Consumo Guardar(Consumo c, out string aviso)
        {
            Validar(c, excludeConsumoId: null);
            _repo.Guardar(c);
            aviso = CalcularAviso(c.GastoId);
            return c;
        }

        public void Actualizar(Consumo c, out string aviso)
        {
            Validar(c, excludeConsumoId: c.Id);
            _repo.Actualizar(c);
            aviso = CalcularAviso(c.GastoId);
        }

        public void Eliminar(int consumoId) => _repo.Eliminar(consumoId);

        private void Validar(Consumo c, int? excludeConsumoId)
        {
            if (c.Monto <= 0)
                throw new ArgumentException("El monto debe ser mayor a cero.");
            if (string.IsNullOrWhiteSpace(c.Descripcion))
                throw new ArgumentException("La descripcion es obligatoria.");
            if (c.Fecha == default) c.Fecha = DateTime.Today;

            var sobre = _repo.ObtenerResumenPorGasto(c.GastoId);
            if (sobre == null)
                throw new ArgumentException("El sobre ya no existe.");

            decimal consumido = _repo.ObtenerConsumido(c.GastoId, excludeConsumoId);
            if (!PresupuestoResumen.Excede(c.Monto, sobre.Limite, consumido)) return;

            decimal disponible = sobre.Limite - consumido;
            throw new ArgumentException(disponible <= 0
                ? $"Ya consumiste todo el sobre \"{sobre.Titulo}\". Ajusta el monto del egreso si necesitas mas."
                : $"Este consumo supera lo que queda en \"{sobre.Titulo}\". Disponible: ${disponible:N2}.");
        }

        private string CalcularAviso(int gastoId)
        {
            var sobre = _repo.ObtenerResumenPorGasto(gastoId);
            if (sobre == null || sobre.Limite <= 0) return null;

            if (sobre.PorcentajeMostrado >= 100)
                return $"Consumiste todo el sobre \"{sobre.Titulo}\".";
            if (sobre.PorcentajeMostrado >= 50)
                return $"Llevas el {sobre.PorcentajeMostrado:N0}% del sobre \"{sobre.Titulo}\". Te quedan ${sobre.Disponible:N2}.";
            return null;
        }
    }
}
