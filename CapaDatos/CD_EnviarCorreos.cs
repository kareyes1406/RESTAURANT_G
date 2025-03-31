using capaDatos;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Configuration;
public class CD_EnvioCorreos
{
    public bool EnviarCorreo(string destinatario, string asunto, int codigoVerificacion)
    {
        try
        {
            // Plantilla HTML del correo
            string cuerpo = $@"
            <html>
            <head>
                <style>
                    body {{
                        font-family: Arial, sans-serif;
                        background-color: #f4f4f4;
                        margin: 0;
                        padding: 0;
                    }}
                    .container {{
                        width: 80%;
                        max-width: 600px;
                        margin: auto;
                        background: #fff;
                        padding: 20px;
                        border-radius: 10px;
                        box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
                        text-align: center;
                    }}
                    .header {{
                        background: #28a745;
                        color: white;
                        padding: 10px;
                        font-size: 22px;
                        border-radius: 10px 10px 0 0;
                    }}
                    .code {{
                        font-size: 24px;
                        font-weight: bold;
                        color: #28a745;
                        margin: 20px 0;
                    }}
                    .footer {{
                        font-size: 14px;
                        color: #777;
                        margin-top: 20px;
                        padding-top: 10px;
                        border-top: 1px solid #ddd;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>¡Bienvenido a ReFood!</div>
                    <p>Gracias por registrarte en nuestra plataforma. Para completar tu registro, usa el siguiente código de verificación:</p>
                    <div class='code'>{codigoVerificacion}</div>
                    <p>Si no solicitaste este código, por favor ignora este mensaje.</p>
                    <div class='footer'>
                        <p>Si tienes alguna pregunta, contáctanos en <a href='mailto:soporte@refood.com'>soporte@refood.com</a></p>
                        <p>¡Gracias por confiar en nosotros! 😊</p>
                    </div>
                </div>
            </body>
            </html>";

            // Configuración del correo
            MailMessage mensaje = new MailMessage();
            mensaje.From = new MailAddress("innovatechpruebas1@gmail.com", "ReFood Soporte");
            mensaje.To.Add(destinatario);
            mensaje.Subject = asunto;
            mensaje.Body = cuerpo;
            mensaje.IsBodyHtml = true;

            // Configuración del servidor SMTP (Gmail)
            SmtpClient smtp = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("innovatechpruebas1@gmail.com", "sdvnuxiafkemrzvs"),
                EnableSsl = true
            };

            // Enviar el correo
            smtp.Send(mensaje);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al enviar el correo: " + ex.Message);
            return false;
        }

    }

    public bool EnviarRecuperar(string destinatario, string asunto, int codigoVerificacion)
    {
        try
        {
            // Plantilla HTML del correo
            string cuerpo = $@"
        <html>
        <head>
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    background-color: #f4f4f4;
                    margin: 0;
                    padding: 0;
                }}
                .container {{
                    width: 80%;
                    max-width: 600px;
                    margin: auto;
                    background: #fff;
                    padding: 20px;
                    border-radius: 10px;
                    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
                    text-align: center;
                }}
                .header {{
                    background: #28a745;
                    color: white;
                    padding: 10px;
                    font-size: 22px;
                    border-radius: 10px 10px 0 0;
                }}
                .code {{
                    font-size: 24px;
                    font-weight: bold;
                    color: #28a745;
                    margin: 20px 0;
                }}
                .footer {{
                    font-size: 14px;
                    color: #777;
                    margin-top: 20px;
                    padding-top: 10px;
                    border-top: 1px solid #ddd;
                }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>Recuperación de Contraseña - ReFood</div>
                <p>Hemos recibido una solicitud para restablecer tu contraseña. Usa el siguiente código para continuar con el proceso:</p>
                <div class='code'>{codigoVerificacion}</div>
                <p>Si no solicitaste este cambio, ignora este mensaje.</p>
                <div class='footer'>
                    <p>Si necesitas ayuda, contáctanos en <a href='mailto:soporte@refood.com'>soporte@refood.com</a></p>
                    <p>¡Gracias por confiar en nosotros! 😊</p>
                </div>
            </div>
        </body>
        </html>";

            // Configuración del correo
            MailMessage mensaje = new MailMessage();
            mensaje.From = new MailAddress("innovatechpruebas1@gmail.com", "ReFood Soporte");
            mensaje.To.Add(destinatario);
            mensaje.Subject = asunto;
            mensaje.Body = cuerpo;
            mensaje.IsBodyHtml = true;

            // Configuración del servidor SMTP (Gmail)
            SmtpClient smtp = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("innovatechpruebas1@gmail.com", "sdvnuxiafkemrzvs"),
                EnableSsl = true
            };

            // Enviar el correo
            smtp.Send(mensaje);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al enviar el correo: " + ex.Message);
            return false;
        }
    }
}



