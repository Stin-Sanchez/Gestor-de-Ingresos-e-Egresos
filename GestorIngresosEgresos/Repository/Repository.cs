using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestorIngresosEgresos.Repository
{
    interface Repository <T>
    {
        List<T> obtenerMovimientos();
        T guardarMovimiento(T t);
        T actualizarMovimiento(T t);
        T obtenerPorId(int id);

        void eliminarMovimiento(int id);
    }
}
