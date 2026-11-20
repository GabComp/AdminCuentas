using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AdministradorDeCuentas.Models.PagoEnLinea;

namespace AdministradorDeCuentas.Services
{
    public sealed class EvoPaymentException : Exception
    {
        public EvoPaymentException(string message) : base(message)
        {
        }
    }

    public sealed class EvoPaymentService
    {
        private readonly HttpClient _client;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EvoPaymentService> _logger;

        public EvoPaymentService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<EvoPaymentService> logger)
        {
            _client = httpClientFactory.CreateClient("EvoClient");
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<EvoSessionResponse> IniciarCheckoutAsync(
            string idOrden,
            decimal importe,
            string? returnUrlPersonalizada,
            string? cancelUrlPersonalizada,
            CancellationToken cancellationToken)
        {
            var initiateUrl = ObtenerConfiguracionObligatoria("Evo:InitiateCheckoutUrl");
            var returnUrl = string.IsNullOrWhiteSpace(returnUrlPersonalizada)
                ? ObtenerConfiguracionObligatoria("Evo:ReturnUrl")
                : returnUrlPersonalizada.Trim();
            var cancelUrl = string.IsNullOrWhiteSpace(cancelUrlPersonalizada)
                ? ObtenerConfiguracionObligatoria("Evo:CancelUrl")
                : cancelUrlPersonalizada.Trim();
            var merchantName = _configuration["Evo:MerchantName"] ?? "COMAPA VICTORIA";

            var merchant = new Dictionary<string, object?>
            {
                ["name"] = merchantName
            };

            var logo = _configuration["Evo:MerchantLogo"];
            if (!string.IsNullOrWhiteSpace(logo))
            {
                merchant["logo"] = logo;
            }

            var merchantUrl = _configuration["Evo:MerchantUrl"];
            if (!string.IsNullOrWhiteSpace(merchantUrl))
            {
                merchant["url"] = merchantUrl;
            }

            var payload = new
            {
                apiOperation = "INITIATE_CHECKOUT",
                checkoutMode = "WEBSITE",
                interaction = new
                {
                    operation = "PURCHASE",
                    locale = "es_MX",
                    returnUrl,
                    cancelUrl,
                    merchant,
                    displayControl = new
                    {
                        billingAddress = "HIDE"
                    }
                },
                order = new
                {
                    id = idOrden,
                    amount = decimal.Round(importe, 2),
                    currency = "MXN",
                    description = "Pago de recibo de agua",
                    reference = $"Recibo-{idOrden}"
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, initiateUrl)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Authorization = CrearAutorizacion();

            using var response = await _client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "EVO rechazó el inicio de sesión para la orden {Orden} con estado {StatusCode}.",
                    idOrden,
                    response.StatusCode);

                throw new EvoPaymentException(
                    "No fue posible iniciar la pasarela de pago. Intenta de nuevo más tarde.");
            }

            EvoSessionResponse? sesion;

            try
            {
                sesion = JsonSerializer.Deserialize<EvoSessionResponse>(responseBody);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Respuesta inválida de EVO al iniciar la orden {Orden}.", idOrden);
                throw new EvoPaymentException(
                    "La pasarela de pago devolvió una respuesta no válida. Intenta de nuevo.");
            }

            if (sesion?.Result != "SUCCESS" || string.IsNullOrWhiteSpace(sesion.Session?.Id))
            {
                _logger.LogWarning(
                    "EVO no creó la sesión para la orden {Orden}. Resultado: {Resultado}.",
                    idOrden,
                    sesion?.Result ?? "sin resultado");

                throw new EvoPaymentException(
                    "La pasarela de pago no pudo iniciar la sesión. Intenta de nuevo más tarde.");
            }

            return sesion;
        }

        public async Task<EvoOrderResponse> ObtenerOrdenAsync(
            string idOrden,
            CancellationToken cancellationToken)
        {
            var retrieveUrl = ObtenerConfiguracionObligatoria("Evo:RetrieveOrderUrl").TrimEnd('/');
            var url = $"{retrieveUrl}/{Uri.EscapeDataString(idOrden)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = CrearAutorizacion();

            using var response = await _client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "EVO rechazó la consulta de la orden {Orden} con estado {StatusCode}.",
                    idOrden,
                    response.StatusCode);

                throw new EvoPaymentException(
                    "No fue posible validar la orden de pago con la pasarela.");
            }

            EvoOrderResponse? orden;

            try
            {
                orden = JsonSerializer.Deserialize<EvoOrderResponse>(responseBody);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Respuesta inválida de EVO al consultar la orden {Orden}.", idOrden);
                throw new EvoPaymentException(
                    "La pasarela devolvió una respuesta no válida al validar el pago.");
            }

            if (orden is null)
            {
                throw new EvoPaymentException(
                    "La pasarela no devolvió información de la orden de pago.");
            }

            return orden;
        }

        private AuthenticationHeaderValue CrearAutorizacion()
        {
            var usuario = ObtenerConfiguracionObligatoria("Evo:User");
            var password = ObtenerConfiguracionObligatoria("Evo:Password");
            var credenciales = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{usuario}:{password}"));

            return new AuthenticationHeaderValue("Basic", credenciales);
        }

        private string ObtenerConfiguracionObligatoria(string clave)
        {
            var valor = _configuration[clave];

            if (string.IsNullOrWhiteSpace(valor))
            {
                throw new InvalidOperationException(
                    $"La configuración '{clave}' es obligatoria para el pago en línea.");
            }

            return valor.Trim();
        }
    }
}
