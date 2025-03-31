using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    public class HistorialPuntos
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public int Puntos { get; set; }
        public string Tipo { get; set; } // "Ganados" o "Usados"
        public DateTime Fecha { get; set; } = DateTime.Now;
    }

}
