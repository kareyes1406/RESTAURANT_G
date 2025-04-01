using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RESTAURANT_G.Tests.Clases
{
   public class ServicioCorreo
    {
        public bool EnviarCorreo(string destinatario, string asunto, int codigo)
        {
            try
            {
                MailMessage mensaje = new MailMessage
                {
                    From = new MailAddress("tuemail@example.com"),
                    Subject = asunto,
                    Body = $"Tu código de verificación es: {codigo}",
                    IsBodyHtml = false
                };
                mensaje.To.Add(destinatario);

                using (SmtpClient smtp = new SmtpClient("smtp.tuservidor.com"))
                {
                    smtp.Credentials = new NetworkCredential("tuemail@example.com", "tucontraseña");
                    smtp.EnableSsl = true;
                    smtp.Send(mensaje);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}