using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace RESTAURANT_G.Tests.Clases
{
    public class UsuarioController : Controller
    {
        private readonly IDatabaseService _databaseService;

        public UsuarioController() : this(new DatabaseService()) { }

        public UsuarioController(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        [HttpPost]
        public JsonResult IniciarSesion(string email, string contraseña)
        {
            try
            {
                string contraseñaHash = _databaseService.ObtenerContraseñaHash(email);

                if (contraseñaHash == null)
                {
                    return Json(new { success = false, message = "El usuario no existe." });
                }

                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytesClave = Encoding.UTF8.GetBytes(contraseña);
                    byte[] hashBytes = sha256.ComputeHash(bytesClave);
                    string claveHash = Convert.ToBase64String(hashBytes);

                    if (claveHash == contraseñaHash)
                    {
                        Session["EmailUsuario"] = email;
                        return Json(new { success = true, message = "Inicio de sesión exitoso." });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Contraseña incorrecta." });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}