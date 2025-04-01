using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web;

namespace RESTAURANT_G.Tests.Clases
{
    public class CodigoVerificacionService
    {
        private readonly IServicioCorreo _servicioCorreo;
        private readonly HttpSessionStateBase _session;
        private readonly Random _random;

        public CodigoVerificacionService(IServicioCorreo servicioCorreo, HttpSessionStateBase session)
        {
            _servicioCorreo = servicioCorreo;
            _session = session;
            _random = new Random();
        }

        public JsonResult EnviarCodigo(string email)
        {
            try
            {
                int codigoVerificacion = _random.Next(100000, 999999);
                _session["CodigoVerificacion"] = codigoVerificacion;
                _session["EmailRegistro"] = email;

                bool enviado = _servicioCorreo.EnviarCorreo(email, "Código de Verificación", codigoVerificacion);
                return new JsonResult { Data = new { success = enviado, message = enviado ? "Código enviado con éxito." : "Error al enviar el código." } };
            }
            catch (Exception ex)
            {
                return new JsonResult { Data = new { success = false, message = "Error: " + ex.Message } };
            }
        }


        public CodigoVerificacionService(HttpSessionStateBase session)
        {
            _session = session;
        }

        public JsonResult VerificarCodigo(string codigo)
        {
            int codigoGuardado = (int)(_session["CodigoVerificacion"] ?? 0);

            if (codigo.Length == 6 && codigoGuardado.ToString() == codigo)
            {
                return new JsonResult { Data = new { success = true, message = "Código correcto. Puedes completar tu registro." } };
            }
            else
            {
                return new JsonResult { Data = new { success = false, message = "Código incorrecto o formato inválido." } };
            }
        }
    }


}

