using capaDatos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;


namespace RestauranteApp.Controllers
{
    [ValidarSesion]
    public class HomeController : Controller
    {
        private readonly CD_Mesas _mesasData = new CD_Mesas();
        private readonly CD_Reservas _reservasData = new CD_Reservas();

        // GET: Reserva
        public ActionResult Index()
        {
            try
            {
                // Obtener el ID del cliente de la sesión
                int clienteId = GetClienteIdFromSession();
                if (clienteId == 0)
                {
                    return RedirectToAction("Index", "Acceso");
                }

                // Obtener las reservas del cliente actual
                var reservasCliente = _reservasData.ObtenerReservasPorCliente(clienteId);

                return View(reservasCliente);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar las reservas: " + ex.Message;
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Reserva/Nueva
        public ActionResult Nueva()
        {
            try
            {
                // Verificar si el usuario está logueado
                int clienteId = GetClienteIdFromSession();
                if (clienteId == 0)
                {
                    return RedirectToAction("Index", "Acceso");
                }

                // Obtener datos del cliente actual
                var clienteData = new CD_Clientes();
                var cliente = clienteData.ObtenerClientePorId(clienteId);

                if (cliente != null && cliente.Count > 0)
                {
                    // Añadir datos del cliente al ViewBag
                    ViewBag.NombreCliente = cliente["Nombre"].ToString();
                    ViewBag.EmailCliente = cliente["Email"].ToString();
                    ViewBag.TelefonoCliente = cliente["Telefono"].ToString();
                }
                else
                {
                    // Si no se encuentran datos del cliente, redirigir al acceso
                    return RedirectToAction("Index", "Acceso");
                }

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar el formulario de reserva: " + ex.Message;
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: Reserva/Nueva
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Nueva(FormCollection form)
        {
            try
            {
                // Verificar si el usuario está logueado
                int clienteId = GetClienteIdFromSession();
                if (clienteId == 0)
                {
                    // Si es una petición AJAX, devolver un mensaje JSON
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Debe iniciar sesión para realizar una reserva." });
                    }
                    return RedirectToAction("Index", "Acceso");
                }

                // Obtener datos del formulario
                int mesaId = Convert.ToInt32(form["MesaId"]);
                DateTime fechaReserva = Convert.ToDateTime(form["FechaReserva"]);
                TimeSpan horaReserva = TimeSpan.Parse(form["HoraReserva"]);
                int cantidadPersonas = Convert.ToInt32(form["CantidadPersonas"]);

                // Obtener el mensaje especial si existe
                string mensajeEspecial = form["MensajeEspecial"] ?? string.Empty;

                // Validar fecha (no puede ser menor a la actual)
                if (fechaReserva.Date < DateTime.Now.Date)
                {
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "La fecha de reserva no puede ser anterior a hoy" });
                    }

                    ModelState.AddModelError("FechaReserva", "La fecha de reserva no puede ser anterior a hoy");
                    ViewBag.MesasDisponibles = _mesasData.ObtenerMesasConEstadoActualizado();
                    return View();
                }

                // Verificar disponibilidad de la mesa
                if (!_reservasData.VerificarDisponibilidadMesa(mesaId, fechaReserva, horaReserva))
                {
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "La mesa seleccionada no está disponible en la fecha y hora indicadas" });
                    }

                    ModelState.AddModelError("MesaId", "La mesa seleccionada no está disponible en la fecha y hora indicadas");
                    ViewBag.MesasDisponibles = _mesasData.ObtenerMesasConEstadoActualizado();
                    return View();
                }

                // Crear la reserva
                bool resultado = _reservasData.CrearReserva(clienteId, mesaId, fechaReserva, horaReserva, cantidadPersonas);

                // Si el formulario se envió por AJAX
                if (Request.IsAjaxRequest())
                {
                    if (resultado)
                    {
                        return Json(new { success = true, message = "¡Reserva creada con éxito!" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "No se pudo crear la reserva. Por favor, intente nuevamente." });
                    }
                }

                // Si el formulario se envió de forma tradicional
                if (resultado)
                {
                    TempData["Success"] = "Reserva creada con éxito";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Error"] = "No se pudo crear la reserva";
                    ViewBag.MesasDisponibles = _mesasData.ObtenerMesasConEstadoActualizado();
                    return View();
                }
            }
            catch (Exception ex)
            {
                // Si el formulario se envió por AJAX
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Error al procesar la reserva: " + ex.Message });
                }

                TempData["Error"] = "Error al procesar la reserva: " + ex.Message;
                return RedirectToAction("Error", "Home");
            }
        }


        // GET: Reserva/BuscarMesas
        public ActionResult BuscarMesas()
        {
            return View();
        }

        // POST: Reserva/BuscarMesas
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BuscarMesas(FormCollection form)
        {
            try
            {
                // Verificar si el usuario está logueado
                int clienteId = GetClienteIdFromSession();
                if (clienteId == 0)
                {
                    return RedirectToAction("Login", "Acceso");
                }

                // Obtener datos del formulario
                int cantidadPersonas = Convert.ToInt32(form["CantidadPersonas"]);
                DateTime fechaReserva = Convert.ToDateTime(form["FechaReserva"]);

                // Buscar mesas disponibles según criterios
                var mesasDisponibles = _mesasData.ObtenerMesasDisponiblesPorCapacidadYFecha(cantidadPersonas, fechaReserva);

                ViewBag.FechaReserva = fechaReserva;
                ViewBag.CantidadPersonas = cantidadPersonas;

                return View("ResultadosBusqueda", mesasDisponibles);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al buscar mesas disponibles: " + ex.Message;
                return RedirectToAction("BuscarMesas");
            }
        }

        // GET: Reserva/Reservar/5
        public ActionResult Reservar(int id, DateTime fecha, string hora, int personas)
        {
            try
            {
                // Verificar si el usuario está logueado
                int clienteId = GetClienteIdFromSession();
                if (clienteId == 0)
                {
                    return RedirectToAction("Login", "Acceso");
                }

                // Obtener datos de la mesa
                var mesa = _mesasData.ObtenerMesaPorId(id);
                if (mesa == null)
                {
                    TempData["Error"] = "Mesa no encontrada";
                    return RedirectToAction("BuscarMesas");
                }

                ViewBag.Mesa = mesa;
                ViewBag.FechaReserva = fecha;
                ViewBag.HoraReserva = hora;
                ViewBag.CantidadPersonas = personas;

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar el formulario de reserva: " + ex.Message;
                return RedirectToAction("BuscarMesas");
            }
        }

        // POST: Reserva/Reservar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reservar(int id, FormCollection form)
        {
            try
            {
                // Verificar si el usuario está logueado
                int clienteId = GetClienteIdFromSession();
                if (clienteId == 0)
                {
                    return RedirectToAction("Login", "Acceso");
                }

                // Obtener datos del formulario
                DateTime fechaReserva = Convert.ToDateTime(form["FechaReserva"]);
                TimeSpan horaReserva = TimeSpan.Parse(form["HoraReserva"]);
                int cantidadPersonas = Convert.ToInt32(form["CantidadPersonas"]);

                // Verificar disponibilidad de la mesa en ese momento
                if (!_reservasData.VerificarDisponibilidadMesa(id, fechaReserva, horaReserva))
                {
                    TempData["Error"] = "La mesa ya no está disponible en esa fecha y hora";
                    return RedirectToAction("BuscarMesas");
                }

                // Crear la reserva
                bool resultado = _reservasData.CrearReserva(clienteId, id, fechaReserva, horaReserva, cantidadPersonas);

                if (resultado)
                {
                    TempData["Success"] = "Reserva realizada con éxito";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Error"] = "No se pudo realizar la reserva";
                    return RedirectToAction("BuscarMesas");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al procesar la reserva: " + ex.Message;
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Reserva/Cancelar/5
        public ActionResult Cancelar(int id)
        {
            try
            {
                // Verificar si el usuario está logueado
                int clienteId = GetClienteIdFromSession();
                if (clienteId == 0)
                {
                    return RedirectToAction("Login", "Acceso");
                }

                // Obtener la reserva
                var reserva = _reservasData.ObtenerReservaPorId(id);
                if (reserva == null)
                {
                    TempData["Error"] = "Reserva no encontrada";
                    return RedirectToAction("Index");
                }

                // Verificar que la reserva pertenezca al cliente actual
                if (Convert.ToInt32(reserva["ClienteId"]) != clienteId)
                {
                    TempData["Error"] = "No tiene permiso para cancelar esta reserva";
                    return RedirectToAction("Index");
                }

                return View(reserva);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar la reserva: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Reserva/Cancelar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarCancelacion(int id)
        {
            try
            {
                // Verificar si el usuario está logueado
                int clienteId = GetClienteIdFromSession();
                if (clienteId == 0)
                {
                    return RedirectToAction("Login", "Acceso");
                }

                // Obtener la reserva
                var reserva = _reservasData.ObtenerReservaPorId(id);
                if (reserva == null)
                {
                    TempData["Error"] = "Reserva no encontrada";
                    return RedirectToAction("Index");
                }

                // Verificar que la reserva pertenezca al cliente actual
                if (Convert.ToInt32(reserva["ClienteId"]) != clienteId)
                {
                    TempData["Error"] = "No tiene permiso para cancelar esta reserva";
                    return RedirectToAction("Index");
                }

                // Cancelar la reserva
                bool resultado = _reservasData.CancelarReserva(id);

                if (resultado)
                {
                    TempData["Success"] = "Reserva cancelada con éxito";
                }
                else
                {
                    TempData["Error"] = "No se pudo cancelar la reserva";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cancelar la reserva: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // GET: Reserva/Detalle/5
        public ActionResult Detalle(int id)
        {
            try
            {
                // Verificar si el usuario está logueado
                int clienteId = GetClienteIdFromSession();
                if (clienteId == 0)
                {
                    return RedirectToAction("Login", "Acceso");
                }

                // Obtener la reserva
                var reserva = _reservasData.ObtenerReservaPorId(id);
                if (reserva == null)
                {
                    TempData["Error"] = "Reserva no encontrada";
                    return RedirectToAction("Index");
                }

                // Verificar que la reserva pertenezca al cliente actual
                if (Convert.ToInt32(reserva["ClienteId"]) != clienteId)
                {
                    TempData["Error"] = "No tiene permiso para ver esta reserva";
                    return RedirectToAction("Index");
                }

                return View(reserva);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar los detalles de la reserva: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // Método auxiliar para obtener el ID del cliente de la sesión
        private int GetClienteIdFromSession()
        {
            if (Session["ClienteId"] != null)
            {
                return Convert.ToInt32(Session["ClienteId"]);
            }
            return 0; // No hay cliente en sesión
        }






        // Método para obtener mesas disponibles según capacidad
        public JsonResult ObtenerMesasDisponibles(int capacidad, DateTime fecha)
        {
            try
            {
                // Verificar que el cliente esté autenticado
                if (Session["ClienteId"] == null)
                {
                    return Json(new List<object>(), JsonRequestBehavior.AllowGet);
                }

                // Usar la clase CD_Mesas para obtener las mesas disponibles
                CD_Mesas mesasData = new CD_Mesas();
                var mesas = mesasData.ObtenerMesasPorCapacidad(capacidad);

                // Filtrar las mesas que tienen reservas para esa fecha/hora
                CD_Reservas reservasData = new CD_Reservas();
                var reservasExistentes = reservasData.ObtenerReservasPorFecha(fecha);

                // Lista para almacenar las mesas que están realmente disponibles
                var mesasDisponibles = new List<Dictionary<string, object>>();

                foreach (var mesa in mesas)
                {
                    int idMesa = Convert.ToInt32(mesa["Id"]);

                    // Si la mesa no tiene reservas para esa fecha o sus reservas no son a la misma hora
                    if (!reservasExistentes.ContainsKey(idMesa) ||
                        reservasExistentes[idMesa].All(r => Convert.ToDateTime(r["FechaReserva"]).Date != fecha.Date))
                    {
                        mesasDisponibles.Add(mesa);
                    }
                }

                return Json(mesasDisponibles, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Registrar el error
                System.Diagnostics.Debug.WriteLine("Error al obtener mesas disponibles: " + ex.Message);
                return Json(new { error = "Error al obtener mesas disponibles" }, JsonRequestBehavior.AllowGet);
            }
        }


       

    }
}