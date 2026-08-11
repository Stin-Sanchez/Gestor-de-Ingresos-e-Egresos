using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Repository;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GestorIngresosEgresos.Controller
{
    public class DeudaController
    {
        private readonly DeudaRepository _repo;

        public DeudaController()
        {
            _repo = new DeudaRepository();
        }

        public List<Deuda> ObtenerTodas() => _repo.ObtenerTodas();

        public List<Deuda> ObtenerActivas() =>
            _repo.ObtenerTodas().Where(d => d.Estado == EstadoDeuda.ACTIVA).ToList();

        public Deuda Guardar(Deuda d)
        {
            if (string.IsNullOrWhiteSpace(d.Nombre))
                throw new ArgumentException("El nombre es obligatorio.");
            if (string.IsNullOrWhiteSpace(d.Acreedor))
                throw new ArgumentException("El acreedor es obligatorio.");
            if (d.MontoOriginal <= 0)
                throw new ArgumentException("El monto debe ser mayor a cero.");
            if (d.FechaInicio == default) d.FechaInicio = DateTime.Today;
            return _repo.Guardar(d);
        }

        public void Eliminar(int id)
        {
            if (id <= 0) throw new ArgumentException("ID invalido.");
            _repo.Eliminar(id);
        }

        // D3: crea Gasto en el periodo activo + actualiza la deuda en una transaccion
        public Gasto RegistrarAbono(int deudaId, int periodoId, int? categoriaId, decimal monto, string descripcion)
        {
            if (monto <= 0) throw new ArgumentException("El monto debe ser mayor a cero.");

            Deuda d = _repo.ObtenerPorId(deudaId)
                ?? throw new Exception("Deuda no encontrada.");

            if (d.Estado == EstadoDeuda.PAGADA)
                throw new InvalidOperationException("La deuda ya esta completamente pagada.");

            if (monto > d.SaldoPendiente)
                throw new ArgumentException($"El abono no puede superar el saldo pendiente (${d.SaldoPendiente:N2}).");

            return _repo.RegistrarAbono(deudaId, periodoId, categoriaId, monto, descripcion);
        }

        public decimal TotalPendiente() =>
            _repo.ObtenerTodas().Where(d => d.Estado == EstadoDeuda.ACTIVA).Sum(d => d.SaldoPendiente);
    }
}
