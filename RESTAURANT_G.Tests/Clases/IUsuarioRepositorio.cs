using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTAURANT_G.Tests.Clases
{
   public interface IUsuarioRepositorio
    {
        void Registrar(string nombre, string correo, string contraseña, string telefono);

        bool ExisteCorreo(string email);
    }
}
