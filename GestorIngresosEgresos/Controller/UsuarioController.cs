using GestorIngresosEgresos.Modelo;
using GestorIngresosEgresos.Repository;
using GestorIngresosEgresos.Util;

namespace GestorIngresosEgresos.Controller
{
    public class UsuarioController
    {
        private readonly UsuarioRepository repository;

        public UsuarioController()
        {
            repository = new UsuarioRepository();
        }

        public Usuario Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            return repository.ObtenerPorCredenciales(username.Trim(), PasswordHelper.Hash(password));
        }
    }
}
