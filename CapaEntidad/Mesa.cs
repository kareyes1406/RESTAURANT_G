using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    public class Mesa
    {

        public int Id { get; set; }
        public int Numero { get; set; }
        public int Capacidad { get; set; } // Personas que caben en la mesa
        public string Ubicacion { get; set; }
        public string Estado { get; set; }
        public bool Disponible { get; set; } = true;
    }
}
