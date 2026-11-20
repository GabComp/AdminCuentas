using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using AdministradorDeCuentas.Models.PagoEnLinea;
using AdministradorDeCuentas.Services;

namespace AdministradorDeCuentas.Controllers
{
    [ApiController]
    [Route("api/pago")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class PagoEnLineaController : ControllerBase
    {
        private readonly RecibosApiService _recibos;
        private readonly EvoPaymentService _evo;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PagoEnLineaController> _logger;

        public PagoEnLineaController(
            RecibosApiService recibos,
            EvoPaymentService evo,
            IConfiguration configuration,
            ILogger<PagoEnLineaController> logger)
        {
            _recibos = recibos;
            _evo = evo;
            _configuration = configuration;
            _logger = logger;
        }

        public sealed class ConsultaAdeudoRequest
        {
            public int Cuenta { get; set; }
        }

        public sealed class ConsultaAdeudoResponse
        {
            public bool Exito { get; set; }
            public bool CuentaValida { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public decimal Adeudo { get; set; }
            public bool TieneNegociacion { get; set; }
            public string Mensaje { get; set; } = string.Empty;
        }

        [HttpPost("obtener-adeudo")]
        [EnableRateLimiting("consulta-adeudo")]
        public async Task<IActionResult> ObtenerAdeudo(
            [FromBody] ConsultaAdeudoRequest? request,
            CancellationToken cancellationToken)
        {
            if (request is null || request.Cuenta <= 0 || request.Cuenta > 999999)
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "Ingresa un número de cuenta válido (máximo 6 dígitos)."
                });
            }

            var adeudo = await _recibos.ObtenerAdeudoAsync(request.Cuenta, cancellationToken);

            if (!adeudo.Exito)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    exito = false,
                    mensaje = adeudo.Mensaje
                });
            }

            return Ok(new ConsultaAdeudoResponse
            {
                Exito = true,
                CuentaValida = adeudo.CuentaValida,
                // La consulta pública solo muestra el nombre enmascarado.
                // El nombre completo se conserva del lado servidor para el comprobante.
                Nombre = EnmascararNombre(adeudo.Nombre),
                Adeudo = adeudo.Adeudo,
                TieneNegociacion = adeudo.TieneNegociacion,
                Mensaje = adeudo.Mensaje
            });
        }

        private static string EnmascararNombre(string? nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return string.Empty;
            }

            var partes = nombre.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return string.Join(' ', partes.Select(parte =>
                parte.Length <= 1
                    ? parte
                    : $"{parte[0]}{new string('*', Math.Min(parte.Length - 1, 6))}"));
        }

        [HttpPost("iniciar")]
        [EnableRateLimiting("iniciar-pago")]
        public async Task<IActionResult> IniciarPago(
            [FromBody] IniciarPagoRequest? request,
            CancellationToken cancellationToken)
        {
            if (request is null || request.Cuenta <= 0 || request.Cuenta > 999999)
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "Ingresa un número de cuenta válido (máximo 6 dígitos)."
                });
            }

            var estadoPago = await _recibos.ObtenerEstadoAsync(cancellationToken);
            var pagoHabilitadoLocal = _configuration.GetValue<bool>("ApiSettings:PermitirAplicarPago");

            // No se abre el checkout si el ambiente no está autorizado para aplicar pagos
            // o si el servicio está suspendido en la configuración operativa.
            if (!pagoHabilitadoLocal || estadoPago.Suspendido)
            {
                var mensaje = !pagoHabilitadoLocal
                    ? "La aplicación de pagos está deshabilitada en este ambiente."
                    : (string.IsNullOrWhiteSpace(estadoPago.Mensaje)
                        ? "Por favor intente más tarde."
                        : estadoPago.Mensaje);

                _logger.LogWarning(
                    "Se bloqueó el inicio de pago para la cuenta {Cuenta}: {Motivo}",
                    request.Cuenta,
                    mensaje);

                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    exito = false,
                    mensaje
                });
            }

            // Se vuelve a consultar el adeudo en el servidor: nunca se confía en el importe del navegador.
            var adeudo = await _recibos.ObtenerAdeudoAsync(request.Cuenta, cancellationToken);

            if (!adeudo.Exito)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    exito = false,
                    mensaje = adeudo.Mensaje
                });
            }

            if (!adeudo.CuentaValida || adeudo.TieneNegociacion || adeudo.Adeudo <= 0)
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = adeudo.Mensaje
                });
            }

            var scriptUrl = _configuration["Evo:CheckoutScriptUrl"];

            if (string.IsNullOrWhiteSpace(scriptUrl))
            {
                _logger.LogError("La configuración 'Evo:CheckoutScriptUrl' no está definida.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    exito = false,
                    mensaje = "La pasarela de pago no está configurada en este momento."
                });
            }

            // El sistema comercial entrega id_ordencompra; se usa como referencia de la orden EVO
            // y como cOrdenCompra en AplicarPago. Si no viene, se genera una referencia de respaldo.
            var idOrden = !string.IsNullOrWhiteSpace(adeudo.IdOrdenCompra)
                ? adeudo.IdOrdenCompra
                : $"WEB{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";

            // Si no se configuraron URLs absolutas, se derivan del host actual
            // (incluye el dominio del App Service o el dominio propio del portal).
            var returnUrl = _configuration["Evo:ReturnUrl"]?.Trim();
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                returnUrl = $"{Request.Scheme}://{Request.Host}/pago-en-linea/retorno";
            }

            var cancelUrl = _configuration["Evo:CancelUrl"]?.Trim();
            if (string.IsNullOrWhiteSpace(cancelUrl))
            {
                cancelUrl = $"{Request.Scheme}://{Request.Host}/pago-en-linea";
            }

            try
            {
                var sesion = await _evo.IniciarCheckoutAsync(
                    idOrden,
                    adeudo.Adeudo,
                    returnUrl,
                    cancelUrl,
                    cancellationToken);

                var pendiente = new PagoPendiente
                {
                    Cuenta = request.Cuenta,
                    Nombre = adeudo.Nombre,
                    Importe = adeudo.Adeudo,
                    IdOrden = idOrden,
                    IdSaldo = adeudo.IdSaldo,
                    Servidor = string.IsNullOrWhiteSpace(adeudo.Servidor)
                        ? estadoPago.Servidor
                        : adeudo.Servidor,
                    SessionId = sesion.Session!.Id!,
                    SessionVersion = sesion.Session.Version ?? string.Empty,
                    SuccessIndicator = sesion.SuccessIndicator ?? string.Empty,
                    CreadoEn = DateTime.UtcNow
                };

                var esHosting = string.Equals(
                    pendiente.Servidor,
                    "hosting",
                    StringComparison.OrdinalIgnoreCase);

                var registro = await _recibos.RegistrarIntentoAsync(
                    new RegistrarIntentoRequest
                    {
                        NCuenta = pendiente.Cuenta,
                        IdSaldo = pendiente.IdSaldo > 0 ? pendiente.IdSaldo : null,
                        COrdenCompra = pendiente.IdOrden,
                        DImporte = pendiente.Importe,
                        SuccessIndicator = pendiente.SuccessIndicator,
                        CSessionId = pendiente.SessionId,
                        CSessionVersion = pendiente.SessionVersion,
                        Servidor = pendiente.Servidor
                    },
                    cancellationToken);

                if (!registro.Exito && esHosting)
                {
                    _logger.LogWarning(
                        "No se pudo registrar el intento de contingencia para la cuenta {Cuenta}: {Mensaje}",
                        request.Cuenta,
                        registro.Mensaje);

                    return StatusCode(StatusCodes.Status502BadGateway, new
                    {
                        exito = false,
                        mensaje = registro.Mensaje
                    });
                }

                if (!registro.Exito)
                {
                    // En modo Comapa el registro previo es informativo: la aplicación
                    // definitiva se hace con AplicarPago al retorno.
                    _logger.LogWarning(
                        "No se pudo registrar el intento de pago para la cuenta {Cuenta} en modo Comapa: {Mensaje}",
                        request.Cuenta,
                        registro.Mensaje);
                }

                HttpContext.Session.SetString(
                    "PagoPendiente",
                    JsonSerializer.Serialize(pendiente));

                HttpContext.Session.Remove("PagoCompletado");

                return Ok(new
                {
                    exito = true,
                    sessionId = pendiente.SessionId,
                    scriptUrl,
                    idOrden,
                    importe = pendiente.Importe,
                    nombre = pendiente.Nombre
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Configuración incompleta de la pasarela de pago.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    exito = false,
                    mensaje = "La pasarela de pago no está configurada correctamente."
                });
            }
            catch (EvoPaymentException ex)
            {
                _logger.LogWarning(ex, "No se pudo iniciar la sesión de EVO para la orden {Orden}.", idOrden);
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    exito = false,
                    mensaje = ex.Message
                });
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Tiempo de espera agotado al iniciar EVO para la orden {Orden}.", idOrden);
                return StatusCode(StatusCodes.Status504GatewayTimeout, new
                {
                    exito = false,
                    mensaje = "La pasarela de pago tardó demasiado. Intenta de nuevo."
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de comunicación con EVO al iniciar la orden {Orden}.", idOrden);
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    exito = false,
                    mensaje = "No fue posible conectar con la pasarela de pago."
                });
            }
        }
    }
}
