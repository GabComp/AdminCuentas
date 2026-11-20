using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace PrototipoComapa.Utilerias
{
    /// <summary>
    /// Envío de correo transaccional con MailKit. Las credenciales SMTP se leen
    /// de configuración (user-secrets / variables de entorno / Key Vault) y
    /// nunca se escriben en el código fuente.
    /// </summary>
    public sealed class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(bool Exito, string Mensaje)> SendEmailAsync(
            string correoDestino,
            string asunto,
            string mensajeCuerpo,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var host = _configuration["Smtp:Host"]?.Trim();
                var usuario = _configuration["Smtp:User"]?.Trim();
                var password = _configuration["Smtp:Password"];
                var remitente = _configuration["Smtp:From"]?.Trim();
                var puerto = _configuration.GetValue<int?>("Smtp:Port") ?? 587;
                var usarSsl = _configuration.GetValue("Smtp:EnableSsl", true);

                if (string.IsNullOrWhiteSpace(host) ||
                    string.IsNullOrWhiteSpace(usuario) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    throw new InvalidOperationException(
                        "La configuración SMTP está incompleta. " +
                        "Defina 'Smtp:Host', 'Smtp:User' y 'Smtp:Password'.");
                }

                var direccionRemitente = string.IsNullOrWhiteSpace(remitente) ? usuario : remitente;

                var mensaje = new MimeMessage();
                mensaje.From.Add(MailboxAddress.Parse(direccionRemitente));
                mensaje.To.Add(MailboxAddress.Parse(correoDestino));
                mensaje.Subject = asunto;
                mensaje.Body = new BodyBuilder { HtmlBody = mensajeCuerpo }.ToMessageBody();

                var opcionesSeguridad = usarSsl
                    ? SecureSocketOptions.Auto
                    : SecureSocketOptions.None;

                using var cliente = new SmtpClient
                {
                    Timeout = 15000
                };

                await cliente.ConnectAsync(host, puerto, opcionesSeguridad, cancellationToken);
                await cliente.AuthenticateAsync(usuario, password, cancellationToken);
                await cliente.SendAsync(mensaje, cancellationToken);
                await cliente.DisconnectAsync(true, cancellationToken);

                return (true, "Se envió un correo con las instrucciones para restablecer la contraseña.");
            }
            catch (SmtpCommandException ex)
            {
                _logger.LogError(
                    ex,
                    "Error SMTP ({StatusCode}) al enviar el correo de restablecimiento.",
                    ex.StatusCode);
                return (false, "No fue posible enviar el correo. Intenta de nuevo más tarde.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al enviar el correo de restablecimiento.");
                return (false, "No fue posible enviar el correo. Intenta de nuevo más tarde.");
            }
        }
    }
}
