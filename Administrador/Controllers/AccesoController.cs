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
using REFOOD.Filters;

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

                Session["AdminCodigoVerificacion"] = codigoVerificacion;
                Session["AdminEmailRegistro"] = email;

                bool enviado = servicioCorreo.EnviarCorreo(email, "Código de Verificación para Administrador", codigoVerificacion);

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
            int codigoGuardado = (int)(Session["AdminCodigoVerificacion"] ?? 0);

            if (codigo.Length == 6 && codigoGuardado.ToString() == codigo)
            {
                return Json(new { success = true, message = "Código correcto. Puedes completar el registro." });
            }
            else
            {
                return Json(new { success = false, message = "Código incorrecto o formato inválido." });
            }
        }

        [HttpPost]
        public JsonResult RegistrarAdministrador(string nombre, string correo, string contraseña)
        {
            try
            {
                if (Session["AdminCodigoVerificacion"] == null || Session["AdminEmailRegistro"].ToString() != correo)
                {
                    return Json(new { success = false, message = "Debe validar el código de verificación antes de registrarse." });
                }

                Console.WriteLine($"Nombre: {nombre}, Correo: {correo}, Contraseña: {contraseña}");

                using (SqlConnection conexion = new SqlConnection(connectionString))
                {
                    conexion.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_RegistrarAdministrador", conexion))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Nombre", nombre);
                        cmd.Parameters.AddWithValue("@Correo", correo);
                        cmd.Parameters.AddWithValue("@Estado", true);

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

                Session["AdminCodigoVerificacion"] = null;
                Session["AdminEmailRegistro"] = null;

                return Json(new { success = true, message = "Registro de administrador exitoso." });
            }
            catch (Exception ex)
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
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Administradores WHERE Correo = @Correo", conexion))
                    {
                        cmd.Parameters.AddWithValue("@Correo", email);
                        int count = (int)cmd.ExecuteScalar();

                        if (count == 0)
                        {
                            return Json(new { success = false, message = "El correo no está registrado como administrador." });
                        }
                    }
                }

                // Generar un código aleatorio de 6 dígitos
                Random rnd = new Random();
                int codigoVerificacion = rnd.Next(100000, 999999);

                // Guardar el código en la sesión temporalmente
                Session["AdminCodigoVerificacion"] = codigoVerificacion;
                Session["AdminEmailRecuperacion"] = email;

                // Enviar el código al correo
                bool enviado = servicioCorreo.EnviarRecuperar(email, "Código de Recuperación para Administrador", codigoVerificacion);

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
            if (Session["AdminCodigoVerificacion"] == null)
            {
                return Json(new { success = false, message = "Código de verificación expirado o no enviado." });
            }

            int codigoGuardado = (int)Session["AdminCodigoVerificacion"];

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

                if (Session["AdminEmailRecuperacion"] == null)
                {
                    return Json(new { success = false, message = "Sesión expirada o inválida." });
                }

                string email = Session["AdminEmailRecuperacion"].ToString().Trim().ToLower(); // Normalización

                Console.WriteLine("Email obtenido de sesión: " + email); // Depuración

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
                        using (SqlCommand cmd = new SqlCommand("sp_CambiarContraseñaAdmin", conexion))
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
                            Console.WriteLine("Resultado del SP: " + resultado); // Depuración

                            if (resultado == 1) // Si se actualizó correctamente
                            {
                                // Limpiar la sesión
                                Session["AdminCodigoVerificacion"] = null;
                                Session["AdminEmailRecuperacion"] = null;

                                return Json(new { success = true, message = "Contraseña de administrador cambiada con éxito." });
                            }
                            else
                            {
                                return Json(new { success = false, message = "El correo no existe en la base de datos de administradores." });
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

                    using (SqlCommand cmd = new SqlCommand("SELECT ContraseñaHash, Estado FROM Administradores WHERE Correo = @Correo", conexion))
                    {
                        cmd.Parameters.AddWithValue("@Correo", email);
                        SqlDataReader reader = cmd.ExecuteReader();

                        if (!reader.Read())
                        {
                            reader.Close();
                            return Json(new { success = false, message = "El administrador no existe." });
                        }

                        // Verificar si el administrador está activo
                        bool estado = (bool)reader["Estado"];
                        if (!estado)
                        {
                            reader.Close();
                            return Json(new { success = false, message = "Cuenta de administrador desactivada. Contacte al soporte técnico." });
                        }

                        // Comparar la contraseña ingresada con la contraseña encriptada en la base de datos
                        string contraseñaHash = reader["ContraseñaHash"].ToString();
                        reader.Close();

                        using (SHA256 sha256 = SHA256.Create())
                        {
                            byte[] bytesClave = Encoding.UTF8.GetBytes(contraseña);
                            byte[] hashBytes = sha256.ComputeHash(bytesClave);
                            string claveHash = Convert.ToBase64String(hashBytes);

                            if (claveHash == contraseñaHash)
                            {
                                // Guardar la sesión de administrador
                                Session["AdminEmail"] = email;

                                // Obtener información adicional del administrador
                                using (SqlCommand cmdInfo = new SqlCommand("SELECT Id, Nombre FROM Administradores WHERE Correo = @Correo", conexion))
                                {
                                    cmdInfo.Parameters.AddWithValue("@Correo", email);
                                    SqlDataReader infoReader = cmdInfo.ExecuteReader();

                                    if (infoReader.Read())
                                    {
                                        Session["AdminId"] = infoReader["Id"];
                                        Session["AdminNombre"] = infoReader["Nombre"];
                                    }
                                    infoReader.Close();
                                }

                                // Registrar el inicio de sesión (opcional)
                                using (SqlCommand cmdLog = new SqlCommand("sp_RegistrarLoginAdmin", conexion))
                                {
                                    cmdLog.CommandType = CommandType.StoredProcedure;
                                    cmdLog.Parameters.AddWithValue("@AdminCorreo", email);
                                    cmdLog.Parameters.AddWithValue("@FechaHora", DateTime.Now);
                                    cmdLog.ExecuteNonQuery();
                                }

                                return Json(new { success = true, message = "Inicio de sesión de administrador exitoso." });
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
            // Limpiar las variables de sesión relacionadas con el administrador
            Session["AdminEmail"] = null;
            Session["AdminId"] = null;
            Session["AdminNombre"] = null;

            // Redirigir a la página de inicio de sesión de administrador
            return RedirectToAction("Index", "Acceso");
        }
    }
}