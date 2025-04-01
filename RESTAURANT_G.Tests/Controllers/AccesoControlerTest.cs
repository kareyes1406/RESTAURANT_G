using Moq;
using NUnit.Framework;
using RESTAURANT_G.Tests.Clases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web;
using System.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;

namespace RESTAURANT_G.Tests.Controllers
{
    [TestFixture]
    class AccesoControlerTest
    {

        [TestFixture]
        public class CodigoVerificacionServiceTests
        {

            private Mock<IServicioCorreo> _mockServicioCorreo;
            private Mock<HttpSessionStateBase> _mockSession;
            private CodigoVerificacionService _service;

            [SetUp]
            public void SetUp()
            {
                _mockServicioCorreo = new Mock<IServicioCorreo>();
                _mockSession = new Mock<HttpSessionStateBase>();

                _service = new CodigoVerificacionService(_mockServicioCorreo.Object, _mockSession.Object);
            }

            [Test]
            public void EnviarCodigo_CorreoEnviado_DevuelveJsonSuccessTrue()
            {
                // Arrange
                string email = "test@example.com";
                _mockServicioCorreo.Setup(s => s.EnviarCorreo(email, "Código de Verificación", It.IsAny<int>())).Returns(true);

                // Act
                JsonResult resultado = _service.EnviarCodigo(email);
                dynamic data = resultado.Data;

                // Assert
                Assert.That(data.success, Is.True);
                Assert.That(data.message, Is.EqualTo("Código enviado con éxito."));
            }

            [Test]
            public void EnviarCodigo_ErrorEnvioCorreo_DevuelveJsonSuccessFalse()
            {
                // Arrange
                string email = "test@example.com";
                _mockServicioCorreo.Setup(s => s.EnviarCorreo(email, "Código de Verificación", It.IsAny<int>())).Returns(false);

                // Act
                JsonResult resultado = _service.EnviarCodigo(email);
                dynamic data = resultado.Data;

                // Assert
                Assert.That(data.success, Is.False);
                Assert.That(data.message, Is.EqualTo("Error al enviar el código."));
            }

            [Test]
            public void EnviarCodigo_Excepcion_DevuelveJsonSuccessFalse()
            {
                // Arrange
                string email = "test@example.com";
                _mockServicioCorreo.Setup(s => s.EnviarCorreo(email, "Código de Verificación", It.IsAny<int>()))
                    .Throws(new Exception("Fallo de conexión"));

                // Act
                JsonResult resultado = _service.EnviarCodigo(email);
                dynamic data = resultado.Data;
                string mensajeError = data.message.ToString(); // Convertir a string

                // Assert
                Assert.That(data.success, Is.False);
                Assert.That(mensajeError, Does.Contain("Error: Fallo de conexión"));
            }




            [Test]
            public void VerificarCodigo_CodigoCorrecto_DevuelveJsonSuccessTrue()
            {
                // Arrange
                string codigoEsperado = "123456";
                _mockSession.Setup(s => s["CodigoVerificacion"]).Returns(123456);

                // Act
                JsonResult resultado = _service.VerificarCodigo(codigoEsperado);
                dynamic data = resultado.Data;

                // Assert
                Assert.That(data.success, Is.True);
                Assert.That(data.message, Is.EqualTo("Código correcto. Puedes completar tu registro."));
            }

            [Test]
            public void VerificarCodigo_CodigoIncorrecto_DevuelveJsonSuccessFalse()
            {
                // Arrange
                string codigoIncorrecto = "654321";
                _mockSession.Setup(s => s["CodigoVerificacion"]).Returns(123456);

                // Act
                JsonResult resultado = _service.VerificarCodigo(codigoIncorrecto);
                dynamic data = resultado.Data;

                // Assert
                Assert.That(data.success, Is.False);
                Assert.That(data.message, Is.EqualTo("Código incorrecto o formato inválido."));
            }

            [Test]
            public void VerificarCodigo_CodigoFormatoInvalido_DevuelveJsonSuccessFalse()
            {
                // Arrange
                string codigoCorto = "123"; // Código con menos de 6 caracteres
                _mockSession.Setup(s => s["CodigoVerificacion"]).Returns(123456);

                // Act
                JsonResult resultado = _service.VerificarCodigo(codigoCorto);
                dynamic data = resultado.Data;

                // Assert
                Assert.That(data.success, Is.False);
                Assert.That(data.message, Is.EqualTo("Código incorrecto o formato inválido."));
            }

            [Test]
            public void VerificarCodigo_SesionVacia_DevuelveJsonSuccessFalse()
            {
                // Arrange
                string codigoIngresado = "123456";
                _mockSession.Setup(s => s["CodigoVerificacion"]).Returns(null);

                // Act
                JsonResult resultado = _service.VerificarCodigo(codigoIngresado);
                dynamic data = resultado.Data;

                // Assert
                Assert.That(data.success, Is.False);
                Assert.That(data.message, Is.EqualTo("Código incorrecto o formato inválido."));
            }
        }

    }
    //_______________________________________________________________________________________________________________________________________________
    [TestFixture]
    public class RegistroServiceTests
    {
        private Mock<IUsuarioRepositorio> _mockUsuarioRepositorio;
        private Mock<IEncriptador> _mockEncriptador;
        private Mock<HttpSessionStateBase> _mockSession;
        private RegistroService _service;

        [SetUp]
        public void SetUp()
        {
            _mockUsuarioRepositorio = new Mock<IUsuarioRepositorio>();
            _mockEncriptador = new Mock<IEncriptador>();
            _mockSession = new Mock<HttpSessionStateBase>();

            _service = new RegistroService(_mockUsuarioRepositorio.Object, _mockEncriptador.Object, _mockSession.Object);
        }

        [Test]
        public void RegistrarUsuario_CodigoNoValidado_DevuelveJsonSuccessFalse()
        {
            // Arrange
            _mockSession.Setup(s => s["CodigoVerificacion"]).Returns(null);

            // Act
            JsonResult resultado = _service.RegistrarUsuario("Juan", "juan@example.com", "123456", "1234567890");
            dynamic data = resultado.Data;

            // Assert
            Assert.That(data.success, Is.False);
            Assert.That(data.message, Is.EqualTo("Debe validar el código de verificación antes de registrarse."));
        }

        [Test]
        public void RegistrarUsuario_RegistroExitoso_DevuelveJsonSuccessTrue()
        {
            // Arrange
            _mockSession.Setup(s => s["CodigoVerificacion"]).Returns(123456);
            _mockSession.Setup(s => s["EmailRegistro"]).Returns("juan@example.com");

            _mockEncriptador.Setup(e => e.Encriptar("123456")).Returns("claveEncriptada");
            _mockUsuarioRepositorio.Setup(r => r.Registrar("Juan", "juan@example.com", "claveEncriptada", "1234567890"));

            // Act
            JsonResult resultado = _service.RegistrarUsuario("Juan", "juan@example.com", "123456", "1234567890");
            dynamic data = resultado.Data;

            // Assert
            Assert.That(data.success, Is.True);
            Assert.That(data.message, Is.EqualTo("Registro exitoso."));
        }
    }

    //_____________________________________________________________________________________________________________________________________________________


    [TestFixture]
    public class RecuperacionServiceTests
    {
        private Mock<IUsuarioRepositorio> _mockUsuarioRepositorio;
        private Mock<IServicioCorreo> _mockServicioCorreo;
        private Mock<HttpSessionStateBase> _mockSession;
        private RecuperacionService _service;

        [SetUp]
        public void SetUp()
        {
            _mockUsuarioRepositorio = new Mock<IUsuarioRepositorio>();
            _mockServicioCorreo = new Mock<IServicioCorreo>();
            _mockSession = new Mock<HttpSessionStateBase>();

            _service = new RecuperacionService(_mockUsuarioRepositorio.Object, _mockServicioCorreo.Object, _mockSession.Object);
        }

        [Test]
        public void EnviarCodigoRecuperacion_CorreoNoRegistrado_DevuelveJsonSuccessFalse()
        {
            // Arrange
            string email = "noexiste@example.com";
            _mockUsuarioRepositorio.Setup(r => r.ExisteCorreo(email)).Returns(false);

            // Act
            JsonResult resultado = _service.EnviarCodigoRecuperacion(email);
            dynamic data = resultado.Data;

            // Assert
            Assert.That(data.success, Is.False);
            Assert.That(data.message, Is.EqualTo("El correo no está registrado."));
        }

        [Test]
        public void EnviarCodigoRecuperacion_CodigoEnviado_DevuelveJsonSuccessTrue()
        {
            // Arrange
            string email = "usuario@example.com";
            _mockUsuarioRepositorio.Setup(r => r.ExisteCorreo(email)).Returns(true);
            _mockServicioCorreo.Setup(s => s.EnviarRecuperar(email, "Código de Recuperación", It.IsAny<int>())).Returns(true);

            // Act
            JsonResult resultado = _service.EnviarCodigoRecuperacion(email);
            dynamic data = resultado.Data;

            // Assert
            Assert.That(data.success, Is.True);
            Assert.That(data.message, Is.EqualTo("Código enviado con éxito."));
        }

        [Test]
        public void EnviarCodigoRecuperacion_ErrorEnvioCorreo_DevuelveJsonSuccessFalse()
        {
            // Arrange
            string email = "usuario@example.com";
            _mockUsuarioRepositorio.Setup(r => r.ExisteCorreo(email)).Returns(true);
            _mockServicioCorreo.Setup(s => s.EnviarRecuperar(email, "Código de Recuperación", It.IsAny<int>())).Returns(false);

            // Act
            JsonResult resultado = _service.EnviarCodigoRecuperacion(email);
            dynamic data = resultado.Data;

            // Assert
            Assert.That(data.success, Is.False);
            Assert.That(data.message, Is.EqualTo("Error al enviar el código."));
        }

        [Test]
        public void EnviarCodigoRecuperacion_Excepcion_DevuelveJsonSuccessFalse()
        {
            // Arrange
            string email = "usuario@example.com";
            _mockUsuarioRepositorio.Setup(r => r.ExisteCorreo(email)).Throws(new Exception("Error en BD"));

            // Act
            JsonResult resultado = _service.EnviarCodigoRecuperacion(email);
            dynamic data = resultado.Data;

            // Assert
            Assert.That(data.success, Is.False);
            Assert.That(data.message.ToString(), Does.Contain("Error: Error en BD"));


        }
    }

    //________________________________________________________________________________________________________________________________________

    [TestFixture]
    public class CodigoRecuperacionServiceTests
    {
        private Mock<HttpSessionStateBase> _mockSession;
        private CodigoRecuperacionService _service;

        [SetUp]
        public void SetUp()
        {
            _mockSession = new Mock<HttpSessionStateBase>();
            _service = new CodigoRecuperacionService(_mockSession.Object);
        }

        [Test]
        public void VerificarCodigoRecuperacion_CodigoNoEnviado_DevuelveJsonSuccessFalse()
        {
            // Arrange
            _mockSession.Setup(s => s["CodigoVerificacion"]).Returns(null);

            // Act
            JsonResult resultado = _service.VerificarCodigoRecuperacion("123456");
            dynamic data = resultado.Data;

            // Assert
            Assert.That(data.success, Is.False);
            Assert.That(data.message, Is.EqualTo("Código de verificación expirado o no enviado."));
        }

        [Test]
        public void VerificarCodigoRecuperacion_CodigoCorrecto_DevuelveJsonSuccessTrue()
        {
            // Arrange
            string codigo = "123456";
            _mockSession.Setup(s => s["CodigoVerificacion"]).Returns(123456);

            // Act
            JsonResult resultado = _service.VerificarCodigoRecuperacion(codigo);
            dynamic data = resultado.Data;

            // Assert
            Assert.That(data.success, Is.True);
            Assert.That(data.message, Is.EqualTo("Código correcto. Ahora puedes cambiar la contraseña."));
        }

        [Test]
        public void VerificarCodigoRecuperacion_CodigoIncorrecto_DevuelveJsonSuccessFalse()
        {
            // Arrange
            _mockSession.Setup(s => s["CodigoVerificacion"]).Returns(123456);

            // Act
            JsonResult resultado = _service.VerificarCodigoRecuperacion("654321");
            dynamic data = resultado.Data;

            // Assert
            Assert.That(data.success, Is.False);
            Assert.That(data.message, Is.EqualTo("Código incorrecto o formato inválido."));
        }

        [Test]
        public void VerificarCodigoRecuperacion_CodigoFormatoIncorrecto_DevuelveJsonSuccessFalse()
        {
            // Arrange
            _mockSession.Setup(s => s["CodigoVerificacion"]).Returns(123456);

            // Act
            JsonResult resultado = _service.VerificarCodigoRecuperacion("abc123");
            dynamic data = resultado.Data;

            // Assert
            Assert.That(data.success, Is.False);
            Assert.That(data.message, Is.EqualTo("Código incorrecto o formato inválido."));
        }
    }

    //_________________________________________________________________________________________________________________________________________
    [TestFixture]
    public class CambioContraseñaServiceTests
    {
        private Mock<HttpSessionStateBase> _mockSession;
        private Mock<IDatabaseService> _mockDatabaseService;
        private CambioContraseñaService _service;

        [SetUp]
        public void SetUp()
        {
            _mockSession = new Mock<HttpSessionStateBase>();
            _mockDatabaseService = new Mock<IDatabaseService>();
            _service = new CambioContraseñaService(_mockSession.Object, _mockDatabaseService.Object);
        }

        [Test]
        public void CambiarContraseña_ContraseñaVacia_DevuelveJsonSuccessFalse()
        {
            // Act
            JsonResult resultado = _service.CambiarContraseña("");
            dynamic data = resultado.Data;

            // Assert
            Assert.That(data.success, Is.False);
            Assert.That(data.message, Is.EqualTo("La nueva contraseña no puede estar vacía."));
        }

        [Test]
        public void CambiarContraseña_SesionExpirada_DevuelveJsonSuccessFalse()
        {
            // Arrange
            _mockSession.Setup(s => s["EmailRecuperacion"]).Returns(null);

            // Act
            JsonResult resultado = _service.CambiarContraseña("NuevaClave123");
            dynamic data = resultado.Data;

            // Assert
            Assert.That(data.success, Is.False);
            Assert.That(data.message, Is.EqualTo("Sesión expirada o inválida."));
        }

        [Test]
        public void CambiarContraseña_CorreoNoExiste_DevuelveJsonSuccessFalse()
        {
            // Arrange
            _mockSession.Setup(s => s["EmailRecuperacion"]).Returns("test@example.com");
            _mockDatabaseService.Setup(db => db.CambiarContraseña(It.IsAny<string>(), It.IsAny<string>())).Returns(0);

            // Act
            JsonResult resultado = _service.CambiarContraseña("NuevaClave123");
            dynamic data = resultado.Data;

            // Assert
            Assert.That(data.success, Is.False);
            Assert.That(data.message, Is.EqualTo("El correo no existe en la base de datos."));
        }

        [Test]
        public void CambiarContraseña_Exito_DevuelveJsonSuccessTrue()
        {
            // Arrange
            _mockSession.Setup(s => s["EmailRecuperacion"]).Returns("test@example.com");
            _mockDatabaseService.Setup(db => db.CambiarContraseña(It.IsAny<string>(), It.IsAny<string>())).Returns(1);

            // Act
            JsonResult resultado = _service.CambiarContraseña("NuevaClave123");
            dynamic data = resultado.Data;

            // Assert
            Assert.That(data.success, Is.True);
            Assert.That(data.message, Is.EqualTo("Contraseña cambiada con éxito."));
        }

        [Test]
        public void CambiarContraseña_ErrorInesperado_DevuelveJsonSuccessFalse()
        {
            // Arrange
            _mockSession.Setup(s => s["EmailRecuperacion"]).Returns("test@example.com");
            _mockDatabaseService.Setup(db => db.CambiarContraseña(It.IsAny<string>(), It.IsAny<string>())).Throws(new Exception("Fallo en el servidor"));

            // Act
            JsonResult resultado = _service.CambiarContraseña("NuevaClave123");
            dynamic data = resultado.Data;

            // Assert
            Assert.That(data.success, Is.False);
            Assert.That(data.message, Does.Contain("Error inesperado: Fallo en el servidor"));
        }
    }

    //________________________________________________________________________________________________________________________________________________
    [TestFixture]
    public class UsuarioControllerTests
    {
        private Mock<IDatabaseService> _mockDatabaseService;
        private Mock<HttpSessionStateBase> _mockSession;
        private UsuarioController _controller;

        [SetUp]
        public void SetUp()
        {
            _mockDatabaseService = new Mock<IDatabaseService>();

            // Mock de HttpContext y Session
            var mockHttpContext = new Mock<HttpContextBase>();
            _mockSession = new Mock<HttpSessionStateBase>();

            mockHttpContext.Setup(ctx => ctx.Session).Returns(_mockSession.Object);

            _controller = new UsuarioController(_mockDatabaseService.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = mockHttpContext.Object }
            };
        }

        [Test]
        public void IniciarSesion_ContraseñaCorrecta_DevuelveJsonSuccessTrue()
        {
            // Arrange
            string email = "test@example.com";
            string contraseña = "password123";

            // Generar hash esperado
            string hashEsperado;
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytesClave = Encoding.UTF8.GetBytes(contraseña);
                byte[] hashBytes = sha256.ComputeHash(bytesClave);
                hashEsperado = Convert.ToBase64String(hashBytes);
            }

            _mockDatabaseService.Setup(db => db.ObtenerContraseñaHash(email)).Returns(hashEsperado);
            _mockSession.Setup(s => s["EmailUsuario"]).Returns(email); // Simula guardar en sesión

            // Act
            JsonResult resultado = _controller.IniciarSesion(email, contraseña);
            dynamic data = resultado.Data;

            // Assert
            Assert.That(data.success, Is.True);
            Assert.That(data.message, Is.EqualTo("Inicio de sesión exitoso."));
        }

        [Test]
        public void IniciarSesion_UsuarioNoExiste_DevuelveJsonSuccessFalse()
        {
            // Arrange
            string email = "noexiste@example.com";
            _mockDatabaseService.Setup(db => db.ObtenerContraseñaHash(email)).Returns((string)null);

            // Act
            JsonResult resultado = _controller.IniciarSesion(email, "password");
            dynamic data = resultado.Data;

            // Assert
            Assert.That(data.success, Is.False);
            Assert.That(data.message, Is.EqualTo("El usuario no existe."));
        }

        [Test]
        public void IniciarSesion_ContraseñaIncorrecta_DevuelveJsonSuccessFalse()
        {
            // Arrange
            string email = "test@example.com";
            string contraseñaIncorrecta = "wrongpassword";
            string contraseñaCorrecta = "password123";

            // Generar hash de la contraseña correcta
            string hashCorrecto;
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytesClave = Encoding.UTF8.GetBytes(contraseñaCorrecta);
                byte[] hashBytes = sha256.ComputeHash(bytesClave);
                hashCorrecto = Convert.ToBase64String(hashBytes);
            }

            _mockDatabaseService.Setup(db => db.ObtenerContraseñaHash(email)).Returns(hashCorrecto);

            // Act
            JsonResult resultado = _controller.IniciarSesion(email, contraseñaIncorrecta);
            dynamic data = resultado.Data;

            // Assert
            Assert.That(data.success, Is.False);
            Assert.That(data.message, Is.EqualTo("Contraseña incorrecta."));
        }

        [Test]
        public void IniciarSesion_ErrorEnBaseDeDatos_DevuelveJsonSuccessFalse()
        {
            // Arrange
            string email = "test@example.com";
            _mockDatabaseService.Setup(db => db.ObtenerContraseñaHash(email)).Throws(new Exception("Error en BD"));

            // Act
            JsonResult resultado = _controller.IniciarSesion(email, "password");
            dynamic data = resultado.Data;

            // Assert
            Assert.That(data.success, Is.False);
            Assert.That(data.message, Does.Contain("Error en BD"));
        }
    }


}