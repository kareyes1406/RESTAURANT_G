using REFOOD.Filters;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace Administrador.Controllers
{
    [AdminAuthentication]
    public class ReservasController : Controller
    {
        private readonly string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["ConexionBD"].ConnectionString;

        // Acción para mostrar todas las reservas
        public ActionResult VerTodasReservas()
        {
            List<ReservaViewModel> listaReservas = new List<ReservaViewModel>();

            try
            {
                using (SqlConnection conexion = new SqlConnection(connectionString))
                {
                    conexion.Open();

                    // Consulta modificada para hacer JOIN con la tabla Cliente
                    string query = @"SELECT R.Id, C.Nombre, C.Telefono, C.Correo, 
                                   R.FechaReserva, R.HoraReserva, R.CantidadPersonas, 
                                   R.Estado,  R.ClienteId 
                                   FROM Reservas R 
                                   INNER JOIN Clientes C ON R.ClienteId = C.Id 
                                   ORDER BY R.FechaReserva DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conexion))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ReservaViewModel reserva = new ReservaViewModel
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    ClienteId = Convert.ToInt32(reader["ClienteId"]),
                                    // Datos del cliente
                                    NombreCliente = reader["Nombre"].ToString(),
                                    Telefono = reader["Telefono"].ToString(),
                                    Email = reader["Correo"].ToString(),
                                    // Datos de la reserva
                                    FechaReserva = Convert.ToDateTime(reader["FechaReserva"]),
                                    HoraReserva = reader["HoraReserva"].ToString(),
                                    NumPersonas = Convert.ToInt32(reader["CantidadPersonas"]),
                                    Estado = reader["Estado"].ToString(),
                                    
                                };

                                listaReservas.Add(reserva);
                            }
                        }
                    }
                }

                return View(listaReservas);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al obtener las reservas: " + ex.Message;
                return View(new List<ReservaViewModel>());
            }
        }

        // Obtener detalle de una reserva específica
        [HttpGet]
        public JsonResult ObtenerDetalleReserva(int id)
        {
            try
            {
                ReservaViewModel reserva = null;

                using (SqlConnection conexion = new SqlConnection(connectionString))
                {
                    conexion.Open();

                    string query = @"SELECT R.Id, C.Nombre, C.Telefono, C.Correo, 
                                   R.FechaReserva, R.HoraReserva, R.CantidadPersonas, 
                                   R.Estado,  R.ClienteId 
                                   FROM Reservas R 
                                   INNER JOIN Clientes C ON R.ClienteId = C.Id 
                                   WHERE R.Id = @Id";

                    using (SqlCommand cmd = new SqlCommand(query, conexion))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                reserva = new ReservaViewModel
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    ClienteId = Convert.ToInt32(reader["ClienteId"]),
                                    // Datos del cliente
                                    NombreCliente = reader["Nombre"].ToString(),
                                    Telefono = reader["Telefono"].ToString(),
                                    Email = reader["Correo"].ToString(),
                                    // Datos de la reserva
                                    FechaReserva = Convert.ToDateTime(reader["FechaReserva"]),
                                    HoraReserva = reader["HoraReserva"].ToString(),
                                    NumPersonas = Convert.ToInt32(reader["CantidadPersonas"]),
                                    Estado = reader["Estado"].ToString(),
                                   
                                };
                            }
                        }
                    }
                }

                if (reserva != null)
                {
                    return Json(new { success = true, data = reserva }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, message = "Reserva no encontrada" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // Eliminar una reserva
        [HttpPost]
        public JsonResult EliminarReserva(int id)
        {
            try
            {
                using (SqlConnection conexion = new SqlConnection(connectionString))
                {
                    conexion.Open();
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM Reservas WHERE Id = @Id", conexion))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        int filasAfectadas = cmd.ExecuteNonQuery();

                        if (filasAfectadas > 0)
                        {
                            return Json(new { success = true });
                        }
                        else
                        {
                            return Json(new { success = false, message = "No se encontró la reserva para eliminar" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // Método para editar reserva (implementación básica)
        public ActionResult EditarReserva(int id)
        {
            // Aquí iría la lógica para cargar y mostrar el formulario de edición
            ViewBag.ReservaId = id;
            return View();
        }

        public class ReservaViewModel
        {
            public int Id { get; set; }
            public int ClienteId { get; set; }
            // Datos del cliente
            public string NombreCliente { get; set; }
            public string Telefono { get; set; }
            public string Email { get; set; }
            // Datos de la reserva
            public DateTime FechaReserva { get; set; }
            public string HoraReserva { get; set; }
            public int NumPersonas { get; set; }
            public string Estado { get; set; }
            public DateTime FechaCreacion { get; set; }
        }
    }
}