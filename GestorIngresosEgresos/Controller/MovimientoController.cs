using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestorIngresosEgresos.Controller
{
    public class MovimientoController
    {
        private RepositoryImp repository;

        public MovimientoController()
        {
            repository = new RepositoryImp();
        }

        // Obtener todos los movimientos
        public List<Movimiento> ObtenerTodosLosMovimientos()
        {
            try
            {
                return repository.obtenerMovimientos();
            }
            catch (Exception e)
            {
                throw new Exception("Error al obtener los movimientos: " + e.Message);
            }
        }

        // Obtener un movimiento por ID
        public Movimiento ObtenerMovimientoPorId(int id)
        {
            Movimiento mov = null;
            try
            {
                if (id <= 0)
                {
                    throw new ArgumentException("El ID debe ser mayor a cero");
                }

                 mov=repository.obtenerPorId(id);

                return mov;
            }
            catch (Exception e)
            {
                throw new Exception("Error al obtener el movimiento: " + e.Message);
            }
        }

        // Guardar un nuevo movimiento
        public Movimiento GuardarMovimiento(Movimiento movimiento)
        {
            try
            {
                // Validaciones
                ValidarMovimiento(movimiento);

                return repository.guardarMovimiento(movimiento);
            }
            catch (Exception e)
            {
                throw new Exception("Error al guardar el movimiento: " + e.Message);
            }
        }

        // Actualizar un movimiento existente
        public Movimiento ActualizarMovimiento(Movimiento movimiento)
        {
            try
            {
                // Validaciones
                if (movimiento.Id <= 0)
                {
                    throw new ArgumentException("El ID del movimiento debe ser válido");
                }

                ValidarMovimiento(movimiento);

                return repository.actualizarMovimiento(movimiento);
            }
            catch (Exception e)
            {
                throw new Exception("Error al actualizar el movimiento: " + e.Message);
            }
        }

        // Eliminar un movimiento
        public void EliminarMovimiento(int id)
        {
            try
            {
                if (id <= 0)
                {
                    throw new ArgumentException("El ID debe ser mayor a cero");
                }

                repository.eliminarMovimiento(id);
            }
            catch (Exception e)
            {
                throw new Exception("Error al eliminar el movimiento: " + e.Message);
            }
        }

        // Calcular el balance total
        public decimal CalcularBalanceTotal()
        {
            try
            {
                List<Movimiento> movimientos = repository.obtenerMovimientos();
                return movimientos.Sum(m => m.Monto);
            }
            catch (Exception e)
            {
                throw new Exception("Error al calcular el balance: " + e.Message);
            }
        }

        // Calcular el total de gastos (egresos)
        public decimal CalcularTotalGastos()
        {
            try
            {
                List<Movimiento> movimientos = repository.obtenerMovimientos();
                return movimientos
                    .Where(m => m.Monto < 0)
                    .Sum(m => Math.Abs(m.Monto));
            }
            catch (Exception e)
            {
                throw new Exception("Error al calcular los gastos: " + e.Message);
            }
        }

        // Calcular el total de ingresos
        public decimal CalcularTotalIngresos()
        {
            try
            {
                List<Movimiento> movimientos = repository.obtenerMovimientos();
                return movimientos
                    .Where(m => m.Monto > 0)
                    .Sum(m => m.Monto);
            }
            catch (Exception e)
            {
                throw new Exception("Error al calcular los ingresos: " + e.Message);
            }
        }

        // Obtener movimientos por tipo
        public List<Movimiento> ObtenerMovimientosPorTipo(TipoMovimiento tipo)
        {
            try
            {
                List<Movimiento> movimientos = repository.obtenerMovimientos();
                return movimientos.Where(m => m.Tipo == tipo).ToList();
            }
            catch (Exception e)
            {
                throw new Exception("Error al filtrar movimientos por tipo: " + e.Message);
            }
        }

        // Obtener movimientos por rango de fechas
        public List<Movimiento> ObtenerMovimientosPorFechas(DateTime fechaInicio, DateTime fechaFin)
        {
            try
            {
                if (fechaInicio > fechaFin)
                {
                    throw new ArgumentException("La fecha de inicio no puede ser mayor a la fecha fin");
                }

                List<Movimiento> movimientos = repository.obtenerMovimientos();
                return movimientos
                    .Where(m => m.Fecha >= fechaInicio && m.Fecha <= fechaFin)
                    .ToList();
            }
            catch (Exception e)
            {
                throw new Exception("Error al filtrar movimientos por fecha: " + e.Message);
            }
        }

        // Obtener resumen financiero
        public ResumenFinanciero ObtenerResumenFinanciero()
        {
            try
            {
                return new ResumenFinanciero
                {
                    BalanceTotal = CalcularBalanceTotal(),
                    TotalIngresos = CalcularTotalIngresos(),
                    TotalGastos = CalcularTotalGastos(),
                    CantidadMovimientos = repository.obtenerMovimientos().Count
                };
            }
            catch (Exception e)
            {
                throw new Exception("Error al obtener el resumen financiero: " + e.Message);
            }
        }

        // Método privado para validar un movimiento
        private void ValidarMovimiento(Movimiento movimiento)
        {
            if (movimiento == null)
            {
                throw new ArgumentNullException("El movimiento no puede ser nulo");
            }

            if (string.IsNullOrWhiteSpace(movimiento.Nombre))
            {
                throw new ArgumentException("El nombre del movimiento es obligatorio");
            }

            if (movimiento.Nombre.Length > 100)
            {
                throw new ArgumentException("El nombre no puede exceder 100 caracteres");
            }

            if (movimiento.Monto == 0)
            {
                throw new ArgumentException("El monto no puede ser cero");
            }

            if (movimiento.Fecha > DateTime.Now)
            {
                throw new ArgumentException("La fecha no puede ser futura");
            }

            if (movimiento.Fecha < new DateTime(2000, 1, 1))
            {
                throw new ArgumentException("La fecha no puede ser anterior al año 2000");
            }

            // Validar que el tipo sea válido
            if (!Enum.IsDefined(typeof(TipoMovimiento), movimiento.Tipo))
            {
                throw new ArgumentException("El tipo de movimiento no es válido");
            }
        }
    

    // Clase auxiliar para el resumen financiero
    public class ResumenFinanciero
    {
        public decimal BalanceTotal { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal TotalGastos { get; set; }
        public int CantidadMovimientos { get; set; }
    }
}


    }

