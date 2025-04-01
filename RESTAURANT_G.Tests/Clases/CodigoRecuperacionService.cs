using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web;

namespace RESTAURANT_G.Tests.Clases
{
    public class CodigoRecuperacionService
    {
        private readonly HttpSessionStateBase _session;

        public CodigoRecuperacionService(HttpSessionStateBase session)
        {
            _session = session;
        }

        public JsonResult VerificarCodigoRecuperacion(string codigo)
        {
            if (_session["CodigoVerificacion"] == null)
            {
                return new JsonResult { Data = new { success = false, message = "Código de verificación expirado o no enviado." } };
            }

            int codigoGuardado = (int)_session["CodigoVerificacion"];

            if (codigo.Length == 6 && codigoGuardado.ToString() == codigo)
            {
                return new JsonResult { Data = new { success = true, message = "Código correcto. Ahora puedes cambiar la contraseña." } };
            }
            else
            {
                return new JsonResult { Data = new { success = false, message = "Código incorrecto o formato inválido." } };
            }
        }
    }
}