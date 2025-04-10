using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Diagnostics;
using capaDatos;
using REFOOD.Filters;

namespace Administrador.Controllers
{
    [AdminAuthentication]
    public class HomeController : Controller
    {
        private readonly CD_Mesas _mesasData = new CD_Mesas();
        private readonly CD_Reservas _reservasData = new CD_Reservas();

        // GET: Home/Index - Vista principal de las mesas
        // GET: Home/Index - Vista principal de las mesas
        public ActionResult Index()
        {
            try
            {
                // Verificar y liberar mesas con reservas expiradas
                _reservasData.LiberarMesasConReservasExpiradas();

                // Log para depuración
                Debug.WriteLine("Obteniendo mesas...");
                List<Dictionary<string, object>> mesas = _mesasData.ObtenerMesasConEstadoActualizado();

                // Verificar si hay mesas
                if (mesas == null || mesas.Count == 0)
                {
                    Debug.WriteLine("No se encontraron mesas");
                    ViewBag.NoHayMesas = true;
                }
                else
                {
                    Debug.WriteLine($"Se encontraron {mesas.Count} mesas");
                }

                return View(mesas);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: " + ex.Message);
                TempData["Error"] = "Error al cargar las mesas: " + ex.Message;
                return View(new List<Dictionary<string, object>>());
            }
        }

        // GET: Home/Create - Formulario para crear una nueva mesa
        public ActionResult Create()
        {
            // Prepara datos para los dropdowns
            CargarDropdowns();

            // Establecer fecha actual mínima
            ViewBag.FechaMinima = DateTime.Now.ToString("yyyy-MM-ddTHH:mm");

            return View();
        }

        // POST: Home/Create - Procesar la creación de una nueva mesa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(FormCollection formCollection)
        {
            try
            {
                // Obtener los valores del formulario
                int numero = Convert.ToInt32(formCollection["Numero"]);
                int capacidad = Convert.ToInt32(formCollection["Capacidad"]);
                string ubicacion = formCollection["Ubicacion"];
                string estado = formCollection["Estado"];
                string disponibleDesde = formCollection["DisponibleDesde"];

                // Validar que no exista una mesa con el mismo número
                if (_mesasData.ExisteMesaConNumero(numero))
                {
                    ModelState.AddModelError("Numero", "Ya existe una mesa con este número.");
                    CargarDropdowns();
                    ViewBag.FechaMinima = DateTime.Now.ToString("yyyy-MM-ddTHH:mm");
                    return View();
                }

                // Validar que la fecha no sea menor a la fecha actual
                if (!DateTime.TryParse(disponibleDesde, out DateTime fechaDisponible)
                    || fechaDisponible < DateTime.Now)
                {
                    ModelState.AddModelError("DisponibleDesde",
                        "La fecha debe ser válida y no puede ser menor a la fecha actual.");
                    CargarDropdowns();
                    ViewBag.FechaMinima = DateTime.Now.ToString("yyyy-MM-ddTHH:mm");
                    return View();
                }

                // Guardar en la base de datos
                if (_mesasData.GuardarMesa(numero, capacidad, ubicacion, estado, fechaDisponible))
                {
                    TempData["Success"] = "Mesa creada correctamente.";
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                CargarDropdowns();
                ViewBag.FechaMinima = DateTime.Now.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
        }

        // GET: Home/Edit/5 - Formulario para editar una mesa existente
        public ActionResult Edit(int id)
        {
            try
            {
                var mesa = _mesasData.ObtenerMesaPorId(id);
                if (mesa == null)
                {
                    TempData["Error"] = "Mesa no encontrada.";
                    return RedirectToAction("Index");
                }

                // Prepara datos para los dropdowns
                CargarDropdowns();
                return View(mesa);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar la mesa: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Home/Edit/5 - Procesar la edición de una mesa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, FormCollection formCollection)
        {
            try
            {
                // Obtener la mesa original para comparar
                var mesaOriginal = _mesasData.ObtenerMesaPorId(id);
                if (mesaOriginal == null)
                {
                    TempData["Error"] = "Mesa no encontrada.";
                    return RedirectToAction("Index");
                }

                // Obtener los valores del formulario
                int numeroFormulario = Convert.ToInt32(formCollection["NumeroMesa"]);
                int numeroOriginal = Convert.ToInt32(mesaOriginal["NumeroMesa"]);
                string disponibleDesdeFormulario = formCollection["DisponibleDesde"];

                // Verificar si DisponibleDesde existe en el objeto mesa
                DateTime fechaOriginal = mesaOriginal.ContainsKey("DisponibleDesde") ?
                    Convert.ToDateTime(mesaOriginal["DisponibleDesde"]) : DateTime.Now;

                // Validar que no se cambie el número de mesa
                if (numeroFormulario != numeroOriginal)
                {
                    ModelState.AddModelError("NumeroMesa", "No se permite cambiar el número de mesa.");
                    CargarDropdowns();
                    return View(mesaOriginal);
                }

                // Validar que la fecha sea válida
                if (!DateTime.TryParse(disponibleDesdeFormulario, out DateTime fechaFormulario))
                {
                    ModelState.AddModelError("DisponibleDesde", "Formato de fecha no válido.");
                    CargarDropdowns();
                    return View(mesaOriginal);
                }

                // Si las validaciones pasan, continuar con la actualización
                int capacidad = Convert.ToInt32(formCollection["Capacidad"]);
                string ubicacion = formCollection["Ubicacion"];
                string estado = formCollection["Estado"];

                // Actualizar en la base de datos
                if (_mesasData.ActualizarMesa(id, numeroOriginal, capacidad, ubicacion, estado, fechaFormulario))
                {
                    TempData["Success"] = "Mesa actualizada correctamente.";
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al actualizar: " + ex.Message;
                CargarDropdowns();
                var mesa = _mesasData.ObtenerMesaPorId(id);
                return View(mesa);
            }
        }

        // POST: Home/CambiarEstado/5 - Cambiar el estado de una mesa
        // En HomeController.cs, método CambiarEstado
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarEstado(int id)
        {
            try
            {
                var mesa = _mesasData.ObtenerMesaPorId(id);
                if (mesa == null)
                {
                    TempData["Error"] = "Mesa no encontrada.";
                    return RedirectToAction("Index");
                }

                // Cambiar estado según el estado actual
                string nuevoEstado = "";
                string mensaje = "";
                int idReserva = -1;

                // Guardar el ID de la reserva si existe
                if (mesa.ContainsKey("ProximaReserva") && mesa["ProximaReserva"] != null)
                {
                    var reserva = (Dictionary<string, object>)mesa["ProximaReserva"];
                    idReserva = Convert.ToInt32(reserva["IdReserva"]);
                }

                switch (mesa["Estado"].ToString())
                {
                    case "disponible":
                        nuevoEstado = "ocupada";
                        mensaje = "Mesa marcada como ocupada.";
                        break;
                    case "ocupada":
                        nuevoEstado = "disponible";
                        mensaje = "Mesa liberada correctamente.";

                        // Si había una reserva atendida, finalizarla
                        if (idReserva != -1)
                        {
                            _reservasData.ActualizarEstadoReserva(idReserva, "Finalizada");
                        }
                        break;
                    case "reservada":
                        nuevoEstado = "ocupada";
                        mensaje = "Reserva confirmada. Mesa ocupada.";

                        // Si está reservada y se confirma, marcar la reserva como atendida
                        if (idReserva != -1)
                        {
                            _reservasData.MarcarReservaComoAtendida(idReserva);
                        }
                        break;
                }

                // Actualizar solo el estado
                if (_mesasData.CambiarEstadoMesa(id, nuevoEstado))
                {
                    TempData["Success"] = mensaje;
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cambiar el estado: " + ex.Message;
                return RedirectToAction("Index");
            }
        }



        // Método auxiliar para cargar los dropdowns
        private void CargarDropdowns()
        {
            ViewBag.Ubicaciones = new List<SelectListItem>{
                new SelectListItem { Text = "Salón Principal", Value = "Salón Principal" },
                new SelectListItem { Text = "Terraza", Value = "Terraza" },
                new SelectListItem { Text = "VIP", Value = "VIP" },
                new SelectListItem { Text = "Barra", Value = "Barra" }
            };

            ViewBag.Estados = new List<SelectListItem>{
                new SelectListItem { Text = "Disponible", Value = "disponible" },
                new SelectListItem { Text = "Ocupada", Value = "ocupada" },
                new SelectListItem { Text = "Reservada", Value = "reservada" }
            };
        }
    }
}