using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web;

namespace RESTAURANT_G.Tests.Clases
{
  public  class RegistroService
    {
        private readonly IUsuarioRepositorio _usuarioRepositorio;
        private readonly IEncriptador _encriptador;
        private readonly HttpSessionStateBase _session;

        public RegistroService(IUsuarioRepositorio usuarioRepositorio, IEncriptador encriptador, HttpSessionStateBase session)
        {
            _usuarioRepositorio = usuarioRepositorio;
            _encriptador = encriptador;
            _session = session;
        }

        public JsonResult RegistrarUsuario(string nombre, string correo, string contraseña, string telefono)
        {
            try
            {
                if (_session["CodigoVerificacion"] == null || _session["EmailRegistro"]?.ToString() != correo)
                {
                    return new JsonResult { Data = new { success = false, message = "Debe validar el código de verificación antes de registrarse." } };
                }

                string claveEncriptada = _encriptador.Encriptar(contraseña);
                _usuarioRepositorio.Registrar(nombre, correo, claveEncriptada, telefono);

                _session["CodigoVerificacion"] = null;
                _session["EmailRegistro"] = null;

                return new JsonResult { Data = new { success = true, message = "Registro exitoso." } };
            }
            catch (SqlException ex)
            {
                return new JsonResult { Data = new { success = false, message = "Error en la base de datos: " + ex.Message } };
            }
        }
    }
}