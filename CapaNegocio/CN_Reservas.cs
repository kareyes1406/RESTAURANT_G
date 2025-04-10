using System;
using System.Collections.Generic;
using capaDatos;
using CapaEntidad;
using System.Linq;

namespace CapaNegocio
{
    public class CN_Reservas
    {
        private readonly CD_Reservas _datosReservas = new CD_Reservas();
        private readonly CD_Mesas _datosMesas = new CD_Mesas(); // Asumiendo que tienes una clase para mesas

        // Método para crear una reserva
        public bool CrearReserva(int clienteId, int mesaId, DateTime fechaReserva, TimeSpan horaReserva, int cantidadPersonas)
        {
            try
            {
                // Verificar disponibilidad antes de crear
                if (!_datosReservas.VerificarDisponibilidadMesa(mesaId, fechaReserva, horaReserva))
                {
                    return false;
                }

                // Crear la reserva en la base de datos
                return _datosReservas.CrearReserva(clienteId, mesaId, fechaReserva, horaReserva, cantidadPersonas);
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Método para buscar una mesa disponible según cantidad de personas
        public int BuscarMesaDisponible(DateTime fechaReserva, TimeSpan horaReserva, int cantidadPersonas)
        {
            try
            {
                // Obtener todas las mesas disponibles para la cantidad de personas
                var mesas = _datosMesas.ObtenerMesasPorCapacidad(cantidadPersonas);

                // Verificar disponibilidad de cada mesa
                foreach (var mesa in mesas)
                {
                    int mesaId = Convert.ToInt32(mesa["Id"]);
                    if (_datosReservas.VerificarDisponibilidadMesa(mesaId, fechaReserva, horaReserva))
                    {
                        return mesaId;
                    }
                }

                return 0; // No hay mesas disponibles
            }
            catch (Exception)
            {
                return 0;
            }
        }

        // Método para verificar disponibilidad general (cualquier mesa)
        public bool VerificarDisponibilidadGeneral(DateTime fechaReserva, TimeSpan horaReserva, int cantidadPersonas)
        {
            try
            {
                // Buscar mesa disponible
                int mesaId = BuscarMesaDisponible(fechaReserva, horaReserva, cantidadPersonas);

                // Si encontramos una mesa con ID > 0, hay disponibilidad
                return mesaId > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Método para obtener todas las reservas
        public List<Dictionary<string, object>> ObtenerTodasLasReservas()
        {
            return _datosReservas.ObtenerTodasLasReservas();
        }

        // Método para obtener reservas por fecha
        public Dictionary<int, List<Dictionary<string, object>>> ObtenerReservasPorFecha(DateTime fecha)
        {
            return _datosReservas.ObtenerReservasPorFecha(fecha);
        }

        // Método para marcar una reserva como atendida
        public bool MarcarReservaComoAtendida(int idReserva)
        {
            return _datosReservas.MarcarReservaComoAtendida(idReserva);
        }

        // Método para cancelar una reserva
        public bool CancelarReserva(int idReserva)
        {
            return _datosReservas.CancelarReserva(idReserva);
        }
    }
}