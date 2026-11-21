using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AdministradorDeCuentas.Filters;
using AdministradorDeCuentas.Models.APIRecibos;
using AdministradorDeCuentas.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace PrototipoComapa.Controllers.Servicios.TramitesDisponibles
{
    [ValidarSesion]
    public class RecibosController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly RecibosAuthService _auth;
        private readonly ILogger<RecibosController> _logger;

        public RecibosController(
            IHttpClientFactory httpClientFactory,
            RecibosAuthService auth,
            ILogger<RecibosController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _auth = auth;
            _logger = logger;
        }

        // GET: RecibosController
        public ActionResult Recibos()
        {
            return View();
        }

        public ActionResult HistorialPagos()
        {
            return View();
        }

        [HttpPost]
        [EnableRateLimiting("recibos")]
        public async Task<IActionResult> ConsultaHistorialPagos([FromBody] DatosValidacion? Jcontent)
        {
            var idCuenta = Jcontent?.filtros?.FirstOrDefault()?.id_cuenta;

            if (idCuenta is null or <= 0)
            {
                return Json(new { ok = false, message = "Debe indicar una cuenta válida." });
            }

            if (!CuentaPerteneceAlUsuario(idCuenta.Value))
            {
                _logger.LogWarning(
                    "El usuario intentó consultar el historial de pagos de una cuenta que no le pertenece ({Cuenta}).",
                    idCuenta);

                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    ok = false,
                    message = "No tienes acceso a esa cuenta."
                });
            }

            var cancellationToken = HttpContext.RequestAborted;

            try
            {
                using var response = await EnviarAsync(
                    "comercial/HistorialPagos",
                    Jcontent!,
                    cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _auth.InvalidarToken();
                    _logger.LogWarning("La API rechazó el token al consultar el historial de pagos.");
                    return ServicioNoDisponible();
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "HistorialPagos respondió {StatusCode} para la cuenta {Cuenta}.",
                        (int)response.StatusCode,
                        idCuenta);

                    return Json(new { ok = false, message = "No fue posible consultar el historial de pagos." });
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var lista = JsonSerializer.Deserialize<List<PagoHistorial>>(
                    body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return Json(new { ok = true, body = lista ?? new List<PagoHistorial>() });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Configuración incompleta al consultar el historial de pagos.");
                return ServicioNoDisponible();
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Tiempo de espera agotado al consultar el historial de pagos de la cuenta {Cuenta}.", idCuenta);
                return StatusCode(StatusCodes.Status504GatewayTimeout, new
                {
                    ok = false,
                    message = "La consulta tardó demasiado. Intenta de nuevo."
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de comunicación al consultar el historial de pagos de la cuenta {Cuenta}.", idCuenta);
                return ServicioNoDisponible();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Respuesta inválida al consultar el historial de pagos de la cuenta {Cuenta}.", idCuenta);
                return Json(new { ok = false, message = "El servicio devolvió una respuesta no válida." });
            }
        }

        [HttpPost]
        [EnableRateLimiting("recibos")]
        public async Task<IActionResult> ConsultaRecibos([FromBody] DatosValidacion? Jcontent)
        {
            var idCuenta = Jcontent?.filtros?.FirstOrDefault()?.id_cuenta;

            if (idCuenta is null or <= 0)
            {
                return Json(new { ok = false, message = "Debe indicar una cuenta válida." });
            }

            if (!CuentaPerteneceAlUsuario(idCuenta.Value))
            {
                _logger.LogWarning(
                    "El usuario intentó consultar recibos de una cuenta que no le pertenece ({Cuenta}).",
                    idCuenta);

                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    ok = false,
                    message = "No tienes acceso a esa cuenta."
                });
            }

            var cancellationToken = HttpContext.RequestAborted;

            try
            {
                using var response = await EnviarAsync(
                    "comercial/HistoriaRecibos",
                    Jcontent!,
                    cancellationToken);

                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _auth.InvalidarToken();
                    _logger.LogWarning("La API de recibos rechazó el token al consultar el historial.");
                    return ServicioNoDisponible();
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "HistoriaRecibos respondió {StatusCode} para la cuenta {Cuenta}.",
                        (int)response.StatusCode,
                        idCuenta);

                    return Json(new { ok = false, message = "No fue posible consultar los recibos." });
                }

                var lista = JsonSerializer.Deserialize<List<ConsultaRecibosResponse>>(
                    body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return Json(new { ok = true, body = lista ?? new List<ConsultaRecibosResponse>() });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Configuración incompleta al consultar recibos.");
                return ServicioNoDisponible();
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Tiempo de espera agotado al consultar recibos de la cuenta {Cuenta}.", idCuenta);
                return StatusCode(StatusCodes.Status504GatewayTimeout, new
                {
                    ok = false,
                    message = "La consulta tardó demasiado. Intenta de nuevo."
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de comunicación al consultar recibos de la cuenta {Cuenta}.", idCuenta);
                return ServicioNoDisponible();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Respuesta inválida al consultar recibos de la cuenta {Cuenta}.", idCuenta);
                return Json(new { ok = false, message = "El servicio devolvió una respuesta no válida." });
            }
        }

        [HttpPost]
        [EnableRateLimiting("recibos")]
        public async Task<IActionResult> ConsultaReciboPDF([FromBody] DatosValidacion? Jcontent)
        {
            var filtro = Jcontent?.filtros?.FirstOrDefault();
            var idRecibo = filtro?.id_recibo;
            var idCuenta = filtro?.id_cuenta;

            if (string.IsNullOrWhiteSpace(idRecibo) || idCuenta is null or <= 0)
            {
                return Json(new
                {
                    ok = false,
                    message = "Debe indicar la cuenta y el recibo que desea descargar."
                });
            }

            if (!CuentaPerteneceAlUsuario(idCuenta.Value))
            {
                _logger.LogWarning(
                    "El usuario intentó descargar un recibo de una cuenta que no le pertenece ({Cuenta}).",
                    idCuenta);

                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    ok = false,
                    message = "No tienes acceso a esa cuenta."
                });
            }

            var cancellationToken = HttpContext.RequestAborted;

            try
            {
                // Se confirma que el recibo pertenece a la cuenta del usuario antes
                // de pedir el PDF. Sin esta validación, un usuario podría cambiar el
                // id_recibo y descargar documentos de otra persona.
                if (!await ReciboPerteneceACuentaAsync(idCuenta.Value, idRecibo, cancellationToken))
                {
                    _logger.LogWarning(
                        "El recibo {Recibo} no pertenece a la cuenta {Cuenta} del usuario.",
                        idRecibo,
                        idCuenta);

                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        ok = false,
                        message = "El recibo no pertenece a la cuenta indicada."
                    });
                }

                using var response = await EnviarAsync(
                    "comercial/ObtnRecibo",
                    Jcontent!,
                    cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _auth.InvalidarToken();
                    _logger.LogWarning("La API de recibos rechazó el token al generar el PDF.");
                    return ServicioNoDisponible();
                }

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning(
                        "ObtnRecibo respondió {StatusCode} para el recibo {Recibo}. Detalle: {Detalle}",
                        (int)response.StatusCode,
                        idRecibo,
                        body);

                    return Json(new { ok = false, message = "No fue posible generar el recibo." });
                }

                var pdfBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

                return Json(new { ok = true, body = pdfBytes });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Configuración incompleta al generar el recibo.");
                return ServicioNoDisponible();
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Tiempo de espera agotado al generar el recibo {Recibo}.", idRecibo);
                return StatusCode(StatusCodes.Status504GatewayTimeout, new
                {
                    ok = false,
                    message = "La generación del recibo tardó demasiado. Intenta de nuevo."
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de comunicación al generar el recibo {Recibo}.", idRecibo);
                return ServicioNoDisponible();
            }
        }

        [HttpGet]
        public IActionResult ObtenerCuentasRecibosUsuario()
        {
            var cuentasJson = HttpContext.Session.GetString("CuentasUsuario");

            if (string.IsNullOrWhiteSpace(cuentasJson))
            {
                return NoContent();
            }

            return Content(cuentasJson, "application/json");
        }

        private bool CuentaPerteneceAlUsuario(int idCuenta)
        {
            var cuentasJson = HttpContext.Session.GetString("CuentasUsuario");

            if (string.IsNullOrWhiteSpace(cuentasJson))
            {
                return false;
            }

            try
            {
                var cuentas = JsonSerializer.Deserialize<List<CuentaSesion>>(
                    cuentasJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return cuentas?.Any(c => c.CUENTA_COMAPA == idCuenta) == true;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "La lista de cuentas de la sesión no es válida.");
                return false;
            }
        }

        private async Task<bool> ReciboPerteneceACuentaAsync(
            int idCuenta,
            string idRecibo,
            CancellationToken cancellationToken)
        {
            var payload = new DatosValidacion
            {
                alias = "VALIDA_CUENTA",
                filtros = new List<Filtro>
                {
                    new() { id_cuenta = idCuenta }
                }
            };

            using var response = await EnviarAsync(
                "comercial/HistoriaRecibos",
                payload,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "No se pudo verificar el recibo {Recibo}: HistoriaRecibos respondió {StatusCode}.",
                    idRecibo,
                    (int)response.StatusCode);

                return false;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var recibos = JsonSerializer.Deserialize<List<ConsultaRecibosResponse>>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return recibos?.Any(r =>
                string.Equals(r.id_recibo, idRecibo, StringComparison.Ordinal)) == true;
        }

        private async Task<HttpResponseMessage> EnviarAsync(
            string ruta,
            object payload,
            CancellationToken cancellationToken)
        {
            var token = await _auth.ObtenerTokenAsync(cancellationToken);
            var client = _httpClientFactory.CreateClient("RecibosClient");

            var request = new HttpRequestMessage(HttpMethod.Post, ruta)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return await client.SendAsync(request, cancellationToken);
        }

        private ObjectResult ServicioNoDisponible() =>
            StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                ok = false,
                message = "El servicio de recibos no está disponible en este momento."
            });

        public sealed class CuentaSesion
        {
            public int CUENTA_COMAPA { get; set; }
        }
    }
}

