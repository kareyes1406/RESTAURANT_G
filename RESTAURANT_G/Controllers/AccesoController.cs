using capaEntidad;
using System;
using capaDatos;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Net.Mail;
using System.Net;

namespace REFOOD.Controllers
{
    public class AccesoController : Controller
    {
        private readonly CD_EnvioCorreos servicioCorreo = new CD_EnvioCorreos();
        private readonly string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["ConexionBD"].ConnectionString;

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Registro()
        {
            return View();
        }

        public ActionResult Recuperar()
        {
            return View();
        }

        [HttpPost]
        public JsonResult EnviarCodigo(string email)
        {
            try
            {
                Random rnd = new Random();
                int codigoVerificacion = rnd.Next(100000, 999999);

                Session["CodigoVerificacion"] = codigoVerificacion;
                Session["EmailRegistro"] = email;

                bool enviado = servicioCorreo.EnviarCorreo(email, "Código de Verificación", codigoVerificacion);

                return Json(new { success = enviado, message = enviado ? "Código enviado con éxito." : "Error al enviar el código." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult VerificarCodigo(string codigo)
        {
            int codigoGuardado = (int)(Session["CodigoVerificacion"] ?? 0);

            if (codigo.Length == 6 && codigoGuardado.ToString() == codigo)
            {
                return Json(new { success = true, message = "Código correcto. Puedes completar tu registro." });
            }
            else
            {
                return Json(new { success = false, message = "Código incorrecto o formato inválido." });
            }
        }

        [HttpPost]
        public JsonResult RegistrarUsuario(string nombre, string correo, string contraseña, string telefono)
        {
            try
            {
                if (Session["CodigoVerificacion"] == null || Session["EmailRegistro"].ToString() != correo)
                {
                    return Json(new { success = false, message = "Debe validar el código de verificación antes de registrarse." });
                }

                Console.WriteLine($"Nombre: {nombre}, Correo: {correo}, Contraseña: {contraseña}, Teléfono: {telefono}");

                using (SqlConnection conexion = new SqlConnection(connectionString))
                {
                    conexion.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_RegistrarCliente", conexion))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Nombre", nombre);
                        cmd.Parameters.AddWithValue("@Correo", correo);
                        cmd.Parameters.AddWithValue("@Telefono", telefono);
                        cmd.Parameters.AddWithValue("@Estado", 1);
                        cmd.Parameters.AddWithValue("@PuntosFidelizacion", 0);

                        // Encriptar la contraseña y enviarla como @Contraseña
                        using (SHA256 sha256 = SHA256.Create())
                        {
                            byte[] bytesClave = Encoding.UTF8.GetBytes(contraseña);
                            byte[] hashBytes = sha256.ComputeHash(bytesClave);
                            string claveHash = Convert.ToBase64String(hashBytes);
                            cmd.Parameters.AddWithValue("@Contraseña", claveHash);
                        }

                        cmd.ExecuteNonQuery();
                    }
                }

                Session["CodigoVerificacion"] = null;
                Session["EmailRegistro"] = null;

                return Json(new { success = true, message = "Registro exitoso." });
            }
            catch (SqlException ex)
            {
                return Json(new { success = false, message = "Error en la base de datos: " + ex.Message });
            }
        }






        [HttpPost]
        public JsonResult EnviarCodigoRecuperacion(string email)
        {
            try
            {
                // Verificar si el correo existe en la base de datos
                using (SqlConnection conexion = new SqlConnection(connectionString))
                {
                    conexion.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Cliente WHERE Correo = @Correo", conexion))
                    {
                        cmd.Parameters.AddWithValue("@Correo", email);
                        int count = (int)cmd.ExecuteScalar();

                        if (count == 0)
                        {
                            return Json(new { success = false, message = "El correo no está registrado." });
                        }
                    }
                }

                // Generar un código aleatorio de 6 dígitos
                Random rnd = new Random();
                int codigoVerificacion = rnd.Next(100000, 999999);

                // Guardar el código en la sesión temporalmente
                Session["CodigoVerificacion"] = codigoVerificacion;
                Session["EmailRecuperacion"] = email;

                // Enviar el código al correo
                bool enviado = servicioCorreo.EnviarRecuperar(email, "Código de Recuperación", codigoVerificacion);

                return Json(new { success = enviado, message = enviado ? "Código enviado con éxito." : "Error al enviar el código." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult VerificarCodigoRecuperacion(string codigo)
        {
            int codigoGuardado = (int)(Session["CodigoVerificacion"] ?? 0);

            if (codigo.Length == 6 && codigoGuardado.ToString() == codigo)
            {
                return Json(new { success = true, message = "Código correcto. Ahora puedes cambiar la contraseña." });
            }
            else
            {
                return Json(new { success = false, message = "Código incorrecto o formato inválido." });
            }
        }

        [HttpPost]
        public JsonResult CambiarContraseña(string nuevaContraseña)
        {
            try
            {
                if (Session["EmailRecuperacion"] == null)
                {
                    return Json(new { success = false, message = "Sesión expirada o inválida." });
                }

                string email = Session["EmailRecuperacion"].ToString();

                // Encriptar la nueva contraseña
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytesClave = Encoding.UTF8.GetBytes(nuevaContraseña);
                    byte[] hashBytes = sha256.ComputeHash(bytesClave);
                    string claveHash = Convert.ToBase64String(hashBytes);

                    // Actualizar la contraseña en la base de datos
                    using (SqlConnection conexion = new SqlConnection(connectionString))
                    {
                        conexion.Open();
                        using (SqlCommand cmd = new SqlCommand("UPDATE Cliente SET Contraseña = @Contraseña WHERE Correo = @Correo", conexion))
                        {
                            cmd.Parameters.AddWithValue("@Contraseña", claveHash);
                            cmd.Parameters.AddWithValue("@Correo", email);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                // Limpiar la sesión
                Session["CodigoVerificacion"] = null;
                Session["EmailRecuperacion"] = null;

                return Json(new { success = true, message = "Contraseña cambiada con éxito." });
            }
            catch (SqlException ex)
            {
                return Json(new { success = false, message = "Error en la base de datos: " + ex.Message });
            }
        }







    }
}

