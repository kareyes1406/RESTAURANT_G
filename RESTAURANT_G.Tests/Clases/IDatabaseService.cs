using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTAURANT_G.Tests.Clases
{
    public interface IDatabaseService
    {
        int CambiarContraseña(string correo, string nuevaContraseña);

        string ObtenerContraseñaHash(string correo);
    }
}
