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
                            font-family: 'Georgia', serif;
                            background-color: #121212;
                            margin: 0;
                            padding: 0;
                            color: #fff;
                        }}
                        .container {{
                            width: 80%;
                            max-width: 600px;
                            margin: auto;
                            background: #1e1e1e;
                            padding: 20px;
                            border-radius: 10px;
                            box-shadow: 0 4px 15px rgba(255, 215, 0, 0.3);
                            text-align: center;
                        }}
                        .header {{
                            background: linear-gradient(to right, #b8860b, #ffd700);
                            color: black;
                            padding: 15px;
                            font-size: 24px;
                            font-weight: bold;
                            border-radius: 10px 10px 0 0;
                        }}
                        .code {{
                            font-size: 28px;
                            font-weight: bold;
                            color: #ffd700;
                            margin: 20px 0;
                        }}
                        .footer {{
                            font-size: 14px;
                            color: #bbb;
                            margin-top: 20px;
                            padding-top: 10px;
                            border-top: 1px solid #444;
                        }}
                        .email-image {{
                            width: 100%;
                            border-radius: 10px 10px 0 0;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <img src='https://i.imgur.com/puD6XNH.png' alt='Bienvenido a LUXNOIRIA' class='email-image'>
                        <div class='header'>Bienvenido a LUXNOIRIA</div>
                        <p>Nos complace darle la bienvenida a una experiencia gastronómica exclusiva. Para completar su registro y acceder a lo mejor de nuestra cocina, ingrese el siguiente código de verificación:</p>
                        <div class='code'>{codigoVerificacion}</div>
                        <p>Si no solicitó este código, simplemente ignore este mensaje.</p>
                        <div class='footer'>
                            <p>Para asistencia, contáctenos en <a href='mailto:innovatechpruebas1@gmail.com' style='color: #ffd700;'>innovatechpruebas1@gmail.com</a></p>
                            <p>Gracias por elegir LUXNOIRIA, donde la elegancia y la gastronomía se fusionan. 🍷✨</p>
                        </div>
                    </div>
                </body>
                </html>";


            // Configuración del correo
            MailMessage mensaje = new MailMessage();
            mensaje.From = new MailAddress("innovatechpruebas1@gmail.com", "LUXNOIRIA Soporte");
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
        {// Plantilla HTML del correo
            string cuerpo = $@"
                        <html>
                        <head>
                            <style>
                                body {{
                                    font-family: 'Georgia', serif;
                                    background-color: #121212;
                                    margin: 0;
                                    padding: 0;
                                    color: #fff;
                                }}
                                .container {{
                                    width: 80%;
                                    max-width: 600px;
                                    margin: auto;
                                    background: #1e1e1e;
                                    padding: 20px;
                                    border-radius: 10px;
                                    box-shadow: 0 4px 15px rgba(255, 215, 0, 0.3);
                                    text-align: center;
                                }}
                                .header {{
                                    background: linear-gradient(to right, #b8860b, #ffd700);
                                    color: black;
                                    padding: 15px;
                                    font-size: 24px;
                                    font-weight: bold;
                                    border-radius: 10px 10px 0 0;
                                }}
                                .logo {{
                                    width: 100%;
                                    max-width: 150px; /* Imagen más pequeña */
                                    margin: 10px auto;
                                    display: block;
                                    border-radius: 5px;
                                }}
                                .code {{
                                    font-size: 28px;
                                    font-weight: bold;
                                    color: #ffd700;
                                    margin: 20px 0;
                                }}
                                .footer {{
                                    font-size: 14px;
                                    color: #bbb;
                                    margin-top: 20px;
                                    padding-top: 10px;
                                    border-top: 1px solid #444;
                                }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <img src='https://i.imgur.com/puD6XNH.png' alt='ReFood Logo' class='logo'>
                                <div class='header'>Recuperación de Contraseña - ReFood</div>
                                <p>Hemos recibido una solicitud para restablecer tu contraseña. Usa el siguiente código para continuar con el proceso:</p>
                                <div class='code'>{codigoVerificacion}</div>
                                <p>Si no solicitaste este cambio, ignora este mensaje.</p>
                                <div class='footer'>
                                    <p>Si necesitas ayuda, contáctanos en <a href='mailto:soporte@refood.com' style='color: #ffd700;'>soporte@refood.com</a></p>
                                    <p>¡Gracias por confiar en nosotros! 😊</p>
                                </div>
                            </div>
                        </body>
                        </html>";



            // Configuración del correo
            MailMessage mensaje = new MailMessage();
            mensaje.From = new MailAddress("innovatechpruebas1@gmail.com", "LUXNOIRIA Soporte");
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



