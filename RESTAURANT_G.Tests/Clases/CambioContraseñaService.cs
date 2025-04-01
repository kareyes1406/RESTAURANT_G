using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web;

namespace RESTAURANT_G.Tests.Clases
{
    public class CambioContraseñaService
    {
        private readonly HttpSessionStateBase _session;
        private readonly IDatabaseService _databaseService;

        public CambioContraseñaService(HttpSessionStateBase session, IDatabaseService databaseService)
        {
            _session = session;
            _databaseService = databaseService;
        }

        public JsonResult CambiarContraseña(string nuevaContraseña)
        {
            try
            {
                if (string.IsNullOrEmpty(nuevaContraseña))
                {
                    return new JsonResult { Data = new { success = false, message = "La nueva contraseña no puede estar vacía." } };
                }

                if (_session["EmailRecuperacion"] == null)
                {
                    return new JsonResult { Data = new { success = false, message = "Sesión expirada o inválida." } };
                }

                string email = _session["EmailRecuperacion"].ToString().Trim().ToLower();

                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytesClave = Encoding.UTF8.GetBytes(nuevaContraseña);
                    byte[] hashBytes = sha256.ComputeHash(bytesClave);
                    string claveHash = Convert.ToBase64String(hashBytes);

                    int resultado = _databaseService.CambiarContraseña(email, claveHash);

                    if (resultado == 1)
                    {
                        _session["CodigoVerificacion"] = null;
                        _session["EmailRecuperacion"] = null;

                        return new JsonResult { Data = new { success = true, message = "Contraseña cambiada con éxito." } };
                    }
                    else
                    {
                        return new JsonResult { Data = new { success = false, message = "El correo no existe en la base de datos." } };
                    }
                }
            }
            catch (Exception ex)
            {
                return new JsonResult { Data = new { success = false, message = "Error inesperado: " + ex.Message } };
            }
        }
    }
}