using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web;

namespace RESTAURANT_G.Tests.Clases
{
    public class RecuperacionService
    {
        private readonly IUsuarioRepositorio _usuarioRepositorio;
        private readonly IServicioCorreo _servicioCorreo;
        private readonly HttpSessionStateBase _session;

        public RecuperacionService(IUsuarioRepositorio usuarioRepositorio, IServicioCorreo servicioCorreo, HttpSessionStateBase session)
        {
            _usuarioRepositorio = usuarioRepositorio;
            _servicioCorreo = servicioCorreo;
            _session = session;
        }

        public JsonResult EnviarCodigoRecuperacion(string email)
        {
            try
            {
                // Verificar si el correo existe en la base de datos
                if (!_usuarioRepositorio.ExisteCorreo(email))
                {
                    return new JsonResult { Data = new { success = false, message = "El correo no está registrado." } };
                }

                // Generar código de verificación
                Random rnd = new Random();
                int codigoVerificacion = rnd.Next(100000, 999999);

                // Guardar en sesión
                _session["CodigoVerificacion"] = codigoVerificacion;
                _session["EmailRecuperacion"] = email;

                // Enviar correo
                bool enviado = _servicioCorreo.EnviarRecuperar(email, "Código de Recuperación", codigoVerificacion);

                return new JsonResult { Data = new { success = enviado, message = enviado ? "Código enviado con éxito." : "Error al enviar el código." } };
            }
            catch (Exception ex)
            {
                return new JsonResult { Data = new { success = false, message = "Error: " + ex.Message } };
            }
        }
    }
}