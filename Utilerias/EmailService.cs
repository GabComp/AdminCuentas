
using PrototipoComapa.Utilerias;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Authentication;

namespace PrototipoComapa.Utilerias
{
    public class EmailService
    {
        public List<Modelos.ListaResultados> SendEmail(string correoDestino, string asunto, string mensajeCuerpo)
        {
            // Aquí iría la lógica para enviar el correo electrónico.
            // Por simplicidad, asumimos que el correo siempre se envía correctamente.
            List<Modelos.ListaResultados> lista = new List<Modelos.ListaResultados>();
            Modelos.ListaResultados modelo = new Modelos.ListaResultados();

            try
            {
                using(SmtpClient client = new SmtpClient("comapavictoria.gob.mx", 587))
                {
                    client.EnableSsl = true;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential("jose.casas@comapavictoria.gob.mx", "J0s3C4s4s$2025");// correo que manda el mensaje
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;// indica que el correo se enviara por SMTP real
                    client.Timeout = 15000;// tiempo de espera

                    System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, SslPolicyErrors) => true;

                    using (MailMessage mail = new MailMessage())
                    {
                        mail.From = new MailAddress("jose.casas@comapavictoria.gob.mx"); // correo remitente
                        mail.To.Add(correoDestino); // correro destinatario
                        mail.Subject = asunto; // asunto del correo
                        mail.Body = mensajeCuerpo; // copntenido del correo
                        mail.IsBodyHtml = true;// permite el formato html
                        mail.Priority = MailPriority.Normal;// establece la prioridad del correo como normal

                        client.Send(mail);
                        modelo.mensaje = $"Correo enviado exitosamente a: {correoDestino} inicie sesion con la clave que se le mando a su correo en lugar de utilizar su contraseña";
                        modelo.exito = true;
                        lista.Add(modelo);

                        return lista;
                    }
                }
            }
            catch (SmtpException smtpEx)
            {

                modelo.mensaje = $"Error SMTP {smtpEx.StatusCode} - {smtpEx.Message}";
                modelo.exito = false;
                lista.Add(modelo);
                return lista;
            }
            catch(AuthenticationException authEx)
            {
                modelo.mensaje = $"Error de autenticación: {authEx.Message}";
                modelo.exito = false;
                lista.Add(modelo);
                return lista;
            }
            catch(Exception ex)
            {
                modelo.mensaje = $"Error inesperado: {ex.Message} Detalles completos: {ex}";
                modelo.exito = false;
                lista.Add(modelo);
                return lista;
            }
            finally
            {
                System.Net.ServicePointManager.ServerCertificateValidationCallback = null;
            }
            
        }
    }
}