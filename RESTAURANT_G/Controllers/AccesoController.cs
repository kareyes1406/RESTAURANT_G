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
            catch (Exception ex)  // Esto captura cualquier excepción
            {

                // Devolver un mensaje más claro para la prueba
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
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Clientes WHERE Correo = @Correo", conexion))
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
            if (Session["CodigoVerificacion"] == null)
            {
                return Json(new { success = false, message = "Código de verificación expirado o no enviado." });
            }

            int codigoGuardado = (int)Session["CodigoVerificacion"];

            Console.WriteLine("Código en sesión: " + codigoGuardado);
            Console.WriteLine("Código ingresado: " + codigo);

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
                if (string.IsNullOrEmpty(nuevaContraseña))
                {
                    return Json(new { success = false, message = "La nueva contraseña no puede estar vacía." });
                }

                if (Session["EmailRecuperacion"] == null)
                {
                    return Json(new { success = false, message = "Sesión expirada o inválida." });
                }

                string email = Session["EmailRecuperacion"].ToString().Trim().ToLower(); // Normalización

                Console.WriteLine("Email obtenido de sesión: " + email); // 👀 Depuración

                // Encriptar la nueva contraseña
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytesClave = Encoding.UTF8.GetBytes(nuevaContraseña);
                    byte[] hashBytes = sha256.ComputeHash(bytesClave);
                    string claveHash = Convert.ToBase64String(hashBytes);

                    // Conexión a SQL Server
                    using (SqlConnection conexion = new SqlConnection(connectionString))
                    {
                        conexion.Open();
                        using (SqlCommand cmd = new SqlCommand("sp_CambiarContraseña", conexion))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@Correo", email);
                            cmd.Parameters.AddWithValue("@NuevaContraseña", claveHash);

                            // Captura el valor de retorno del SP
                            SqlParameter returnParameter = new SqlParameter("@ReturnVal", SqlDbType.Int);
                            returnParameter.Direction = ParameterDirection.ReturnValue;
                            cmd.Parameters.Add(returnParameter);

                            cmd.ExecuteNonQuery();

                            int resultado = (int)returnParameter.Value; // Valor devuelto por el SP
                            Console.WriteLine("Resultado del SP: " + resultado); // 👀 Depuración

                            if (resultado == 1) // Si se actualizó correctamente
                            {
                                // Limpiar la sesión
                                Session["CodigoVerificacion"] = null;
                                Session["EmailRecuperacion"] = null;

                                return Json(new { success = true, message = "Contraseña cambiada con éxito." });
                            }
                            else
                            {
                                return Json(new { success = false, message = "El correo no existe en la base de datos." });
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                return Json(new { success = false, message = "Error en la base de datos: " + ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error inesperado: " + ex.Message });
            }
        }







        [HttpPost]
        public JsonResult IniciarSesion(string email, string contraseña)
        {
            try
            {
                using (SqlConnection conexion = new SqlConnection(connectionString))
                {
                    conexion.Open();

                    using (SqlCommand cmd = new SqlCommand("SELECT ID, Contraseña FROM Clientes WHERE Correo = @Correo", conexion))
                    {
                        cmd.Parameters.AddWithValue("@Correo", email);
                        SqlDataReader reader = cmd.ExecuteReader();

                        if (!reader.Read())
                        {
                            return Json(new { success = false, message = "El usuario no existe." });
                        }

                        int clienteId = Convert.ToInt32(reader["ID"]);
                        string contraseñaHash = reader["Contraseña"].ToString();
                        reader.Close();

                        // Comparar la contraseña ingresada con la contraseña encriptada en la base de datos
                        using (SHA256 sha256 = SHA256.Create())
                        {
                            byte[] bytesClave = Encoding.UTF8.GetBytes(contraseña);
                            byte[] hashBytes = sha256.ComputeHash(bytesClave);
                            string claveHash = Convert.ToBase64String(hashBytes);

                            if (claveHash == contraseñaHash)
                            {
                                // Guardar la sesión de usuario y el ID del cliente
                                Session["EmailUsuario"] = email;
                                Session["ClienteId"] = clienteId;

                                return Json(new { success = true, message = "Inicio de sesión exitoso." });
                            }
                            else
                            {
                                return Json(new { success = false, message = "Contraseña incorrecta." });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        public ActionResult CerrarSesion()
        {
            // Limpiar las variables de sesión relacionadas con el usuario
            Session["EmailUsuario"] = null;
            Session["ClienteId"] = null;

            // Redirigir a la página de inicio de sesión
            return RedirectToAction("Index", "Acceso");
        }

    }

}
