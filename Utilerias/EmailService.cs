using System.Net.Http.Headers;
using System.Net.Http.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace PrototipoComapa.Utilerias
{
    /// <summary>
    /// Envío de correo transaccional. Usa la API HTTP de Resend cuando
    /// 'Resend:ApiKey' está configurada (recomendado, sale por 443) y, si no,
    /// cae a SMTP con MailKit. Las credenciales se leen de configuración
    /// (user-secrets / variables de entorno) y nunca se escriben en el código.
    /// </summary>
    public sealed class EmailService
    {
        private const string ResendEndpoint = "https://api.resend.com/emails";

        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<(bool Exito, string Mensaje)> SendEmailAsync(
            string correoDestino,
            string asunto,
            string mensajeCuerpo,
            CancellationToken cancellationToken = default)
        {
            var resendKey = _configuration["Resend:ApiKey"]?.Trim();

            if (!string.IsNullOrWhiteSpace(resendKey))
            {
                return await EnviarPorResendAsync(
                    resendKey,
                    correoDestino,
                    asunto,
                    mensajeCuerpo,
                    cancellationToken);
            }

            return await EnviarPorSmtpAsync(
                correoDestino,
                asunto,
                mensajeCuerpo,
                cancellationToken);
        }

        private async Task<(bool Exito, string Mensaje)> EnviarPorResendAsync(
            string apiKey,
            string correoDestino,
            string asunto,
            string mensajeCuerpo,
            CancellationToken cancellationToken)
        {
            var remitente = PrimerValor(
                _configuration["Resend:From"],
                _configuration["Smtp:From"],
                _configuration["Smtp:User"]);

            if (string.IsNullOrWhiteSpace(remitente))
            {
                _logger.LogError(
                    "Falta 'Resend:From' (o 'Smtp:From') para enviar correo con Resend.");

                return (false, "No fue posible enviar el correo. Falta configurar el remitente.");
            }

            try
            {
                var cliente = _httpClientFactory.CreateClient("Resend");
                cliente.Timeout = TimeSpan.FromSeconds(20);

                using var solicitud = new HttpRequestMessage(HttpMethod.Post, ResendEndpoint)
                {
                    Content = JsonContent.Create(new
                    {
                        from = remitente,
                        to = new[] { correoDestino },
                        subject = asunto,
                        html = mensajeCuerpo
                    })
                };

                solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                using var respuesta = await cliente.SendAsync(solicitud, cancellationToken);
                var cuerpo = await respuesta.Content.ReadAsStringAsync(cancellationToken);

                if (!respuesta.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Resend respondió {StatusCode} al enviar a {Correo}: {Cuerpo}",
                        (int)respuesta.StatusCode,
                        correoDestino,
                        cuerpo);

                    return (false, "No fue posible enviar el correo. Intenta de nuevo más tarde.");
                }

                return (true, "Correo enviado correctamente.");
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError("Resend tardó demasiado al enviar a {Correo}.", correoDestino);
                return (false, "El envío del correo tardó demasiado. Intenta de nuevo.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "No se pudo conectar con Resend para enviar a {Correo}.", correoDestino);
                return (false, "No fue posible conectar con el servicio de correo.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al enviar correo con Resend a {Correo}.", correoDestino);
                return (false, "No fue posible enviar el correo. Intenta de nuevo más tarde.");
            }
        }

        private async Task<(bool Exito, string Mensaje)> EnviarPorSmtpAsync(
            string correoDestino,
            string asunto,
            string mensajeCuerpo,
            CancellationToken cancellationToken)
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
                        "La configuración SMTP está incompleta y no hay 'Resend:ApiKey'. " +
                        "Defina SMTP o la API key de Resend.");
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

                return (true, "Correo enviado correctamente.");
            }
            catch (SmtpCommandException ex)
            {
                _logger.LogError(
                    ex,
                    "Error SMTP ({StatusCode}) al enviar el correo.",
                    ex.StatusCode);

                return (false, "No fue posible enviar el correo. Intenta de nuevo más tarde.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al enviar el correo.");
                return (false, "No fue posible enviar el correo. Intenta de nuevo más tarde.");
            }
        }

        private static string PrimerValor(params string?[] valores) =>
            valores.FirstOrDefault(valor => !string.IsNullOrWhiteSpace(valor))?.Trim() ?? string.Empty;
    }
}
