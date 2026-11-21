using Microsoft.AspNetCore.Mvc;

namespace AdministradorDeCuentas.Controllers
{
    /// <summary>
    /// Endpoints de monitoreo para App Service / balanceadores.
    /// - GET /health      : liveness (el proceso responde).
    /// - GET /health/ready: readiness (valida la dependencia con la API).
    /// </summary>
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private const string NombreServicio = "AdministradorDeCuentas";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            IHttpClientFactory httpClientFactory,
            ILogger<HealthController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Liveness() =>
            Ok(new
            {
                status = "Healthy",
                service = NombreServicio,
                timestamp = DateTimeOffset.UtcNow
            });

        [HttpGet("ready")]
        public async Task<IActionResult> Readiness(CancellationToken cancellationToken)
        {
            var estadoApi = "Healthy";
            var esSaludable = true;

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(3));

                var client = _httpClientFactory.CreateClient("ApiGeneralClient");
                using var response = await client.GetAsync("health", cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    esSaludable = false;
                    estadoApi = "Unhealthy";
                    _logger.LogWarning(
                        "La API de compatibilidad respondió {StatusCode} en el readiness del portal.",
                        (int)response.StatusCode);
                }
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                esSaludable = false;
                estadoApi = "Unhealthy";
                _logger.LogWarning("Tiempo de espera agotado al validar la API en el readiness del portal.");
            }
            catch (Exception ex)
            {
                esSaludable = false;
                estadoApi = "Unhealthy";
                _logger.LogError(ex, "No se pudo validar la API en el readiness del portal.");
            }

            return esSaludable
                ? Ok(new
                {
                    status = "Healthy",
                    service = NombreServicio,
                    dependencies = new { api = estadoApi },
                    timestamp = DateTimeOffset.UtcNow
                })
                : StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "Unhealthy",
                    service = NombreServicio,
                    dependencies = new { api = estadoApi },
                    timestamp = DateTimeOffset.UtcNow
                });
        }
    }
}
