using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTAURANT_G.Tests.Clases
{
  public  interface IServicioCorreo
    {
        bool EnviarCorreo(string destinatario, string asunto, int codigo);

        bool EnviarRecuperar(string email, string asunto, int codigo);
    }
}
