using System;

namespace capaEntidad
{
    public class Cliente
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }  // Consistente con el SP
        public string Telefono { get; set; }
        public int PuntosAcumulados { get; set; }
        public string ContraseñaHash { get; set; } // Nombre más claro
        public DateTime FechaRegistro { get; set; } = DateTime.Now; // Fecha de registro automática
        public bool Estado { get; set; } = true; // Activo por defecto
    }
}
