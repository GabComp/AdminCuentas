using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AdministradorDeCuentas.Models.Constancias;
using AdministradorDeCuentas.Models.PagoEnLinea;
using AdministradorDeCuentas.Services;
using PrototipoComapa.Extensions;
using PrototipoComapa.Utilerias;
using static PrototipoComapa.Models.UsuarioWeb.UsuarioWebModel;

namespace AdministradorDeCuentas.Pages
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class PagoRetornoModel : PageModel
    {
        private readonly EvoPaymentService _evo;
        private readonly RecibosApiService _recibos;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly ILogger<PagoRetornoModel> _logger;

        public PagoRetornoModel(
            EvoPaymentService evo,
            RecibosApiService recibos,
            IConfiguration configuration,
            EmailService emailService,
            ILogger<PagoRetornoModel> logger)
        {
            _evo = evo;
            _recibos = recibos;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
        }

        public string? MensajeError { get; private set; }

        public async Task<IActionResult> OnGetAsync(
            string? resultIndicator,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(resultIndicator))
            {
                MensajeError = "No se recibió el indicador de pago de la pasarela.";
                return Page();
            }

            var pendienteJson = HttpContext.Session.GetString("PagoPendiente");

            if (string.IsNullOrWhiteSpace(pendienteJson))
            {
                MensajeError = "No hay un pago pendiente en esta sesión. Inicia la consulta nuevamente.";
                return Page();
            }

            PagoPendiente? pendiente;

            try
            {
                pendiente = JsonSerializer.Deserialize<PagoPendiente>(pendienteJson);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "No se pudo leer la orden pendiente de la sesión.");
                MensajeError = "La información del pago no es válida. Inicia la consulta nuevamente.";
                return Page();
            }

            if (pendiente is null)
            {
                MensajeError = "La información del pago no es válida. Inicia la consulta nuevamente.";
                return Page();
            }

            if (pendiente.Cuenta <= 0 ||
                pendiente.Importe <= 0 ||
                string.IsNullOrWhiteSpace(pendiente.IdOrden) ||
                string.IsNullOrWhiteSpace(pendiente.SuccessIndicator))
            {
                HttpContext.Session.Remove("PagoPendiente");
                MensajeError = "La información del pago no es válida. Inicia la consulta nuevamente.";
                return Page();
            }

            var creado = pendiente.CreadoEn.Kind == DateTimeKind.Utc
                ? pendiente.CreadoEn
                : pendiente.CreadoEn.ToUniversalTime();

            if (creado != default && DateTime.UtcNow - creado > TimeSpan.FromMinutes(60))
            {
                HttpContext.Session.Remove("PagoPendiente");
                MensajeError =
                    "La orden de pago expiró. Si el banco autorizó el cargo, conserva tu comprobante " +
                    "y comunícate a la sucursal.";
                return Page();
            }

            if (!string.Equals(
                    pendiente.SuccessIndicator,
                    resultIndicator,
                    StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "El indicador de retorno no coincide con la orden {Orden}.",
                    pendiente.IdOrden);

                MensajeError = "El pago no corresponde a la orden iniciada en este sitio.";
                return Page();
            }

            if (!_configuration.GetValue<bool>("ApiSettings:PermitirAplicarPago"))
            {
                _logger.LogWarning(
                    "Se recibió un retorno de pago para la orden {Orden}, pero la aplicación de pagos está deshabilitada.",
                    pendiente.IdOrden);

                MensajeError =
                    "La aplicación de pagos está deshabilitada en este ambiente. " +
                    "La pasarela pudo haber autorizado el cobro; conserva tu comprobante y contacta a la sucursal.";
                return Page();
            }

            var estadoPago = await _recibos.ObtenerEstadoAsync(cancellationToken);

            if (estadoPago.Suspendido)
            {
                _logger.LogWarning(
                    "Se recibió un retorno de pago para la orden {Orden}, pero el servicio está suspendido.",
                    pendiente.IdOrden);

                MensajeError =
                    "El pago en línea está suspendido en este momento. " +
                    "La pasarela pudo haber autorizado el cobro; conserva tu comprobante y contacta a la sucursal.";
                return Page();
            }

            EvoOrderResponse orden;

            try
            {
                orden = await _evo.ObtenerOrdenAsync(pendiente.IdOrden, cancellationToken);
            }
            catch (EvoPaymentException ex)
            {
                MensajeError = ex.Message;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error inesperado al consultar la orden {Orden} en EVO.",
                    pendiente.IdOrden);

                MensajeError =
                    "No fue posible validar la orden con la pasarela. " +
                    "Conserva tu comprobante y contacta a la sucursal.";
                return Page();
            }

            if (!string.Equals(orden.Result, "SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                MensajeError = "La pasarela no confirmó el pago como exitoso.";
                return Page();
            }

            if (!string.Equals(orden.Id, pendiente.IdOrden, StringComparison.OrdinalIgnoreCase))
            {
                MensajeError = "La orden devuelta por la pasarela no coincide con la orden iniciada.";
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(orden.Currency) &&
                !string.Equals(orden.Currency, "MXN", StringComparison.OrdinalIgnoreCase))
            {
                MensajeError = "La moneda de la orden no es válida.";
                return Page();
            }

            var montoOrden = orden.Amount ?? orden.MerchantAmount;

            if (montoOrden.HasValue &&
                Math.Abs(montoOrden.Value - pendiente.Importe) > 0.01m)
            {
                _logger.LogWarning(
                    "El importe de la orden {Orden} no coincide. EVO: {MontoEvo}; esperado: {MontoEsperado}.",
                    pendiente.IdOrden,
                    montoOrden.Value,
                    pendiente.Importe);

                MensajeError = "El importe autorizado por la pasarela no coincide con el adeudo consultado.";
                return Page();
            }

            var tarjeta = orden.SourceOfFunds?.Provided?.Card;
            var numeroTarjeta = tarjeta?.Number ?? string.Empty;
            var ultimosDigitos = numeroTarjeta.Length >= 4
                ? numeroTarjeta[^4..]
                : numeroTarjeta;

            var fechaPago = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(orden.LastUpdatedTime) &&
                DateTime.TryParse(
                    orden.LastUpdatedTime,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var fechaOrden))
            {
                fechaPago = fechaOrden;
            }

            if (string.Equals(pendiente.Tipo, "constancia", StringComparison.OrdinalIgnoreCase))
            {
                var pagoConstancia = await _recibos.AplicarPagoConstanciaAsync(
                    new ConstanciaPagoApiRequest
                    {
                        IdConstancia = pendiente.IdConstancia,
                        IdCuenta = pendiente.Cuenta,
                        Referencia = pendiente.IdOrden
                    },
                    cancellationToken);

                if (!pagoConstancia.Exito)
                {
                    MensajeError = pagoConstancia.Mensaje;
                    return Page();
                }

                var completadoConstancia = new PagoCompletado
                {
                    Tipo = "constancia",
                    Cuenta = pendiente.Cuenta,
                    Nombre = pendiente.Nombre,
                    Importe = pagoConstancia.Total > 0 ? pagoConstancia.Total : pendiente.Importe,
                    Folio = pagoConstancia.FolioPago,
                    IdOrden = pendiente.IdOrden,
                    IdConstancia = pendiente.IdConstancia,
                    FolioConstancia = string.IsNullOrWhiteSpace(pagoConstancia.Folio)
                        ? pendiente.FolioConstancia
                        : pagoConstancia.Folio,
                    MarcaTarjeta = tarjeta?.Brand ?? string.Empty,
                    UltimosDigitos = ultimosDigitos,
                    Fecha = fechaPago
                };

                HttpContext.Session.SetString(
                    "PagoCompletado",
                    JsonSerializer.Serialize(completadoConstancia));

                HttpContext.Session.Remove("PagoPendiente");

                return RedirectToPage("/PagoCompletado");
            }

            var usuarioPago = _configuration["ApiSettings:RecibosUsuario"]?.Trim();

            var solicitud = new AplicarPagoApiRequest
            {
                CIdUsuario = string.IsNullOrWhiteSpace(usuarioPago) ? "U0293" : usuarioPago,
                NCuenta = pendiente.Cuenta,
                DImporte = pendiente.Importe,
                NIdFormaPago = _configuration.GetValue<int?>("ApiSettings:RecibosFormaPago") ?? 4,
                COrdenCompra = pendiente.IdOrden,
                SuccessIndicator = resultIndicator,
                CSessionId = pendiente.SessionId,
                CardBrand = tarjeta?.Brand ?? string.Empty,
                CardFundingMethod = tarjeta?.FundingMethod ?? string.Empty,
                CardNameOnCard = string.IsNullOrWhiteSpace(tarjeta?.NameOnCard)
                    ? "TITULAR"
                    : tarjeta!.NameOnCard!,
                CardNumber = ultimosDigitos,
                CardIssuer = tarjeta?.Issuer ?? string.Empty,
                Fecha = fechaPago,
                IdSaldo = pendiente.IdSaldo > 0 ? pendiente.IdSaldo : null,
                Servidor = pendiente.Servidor
            };

            var resultado = await _recibos.AplicarPagoAsync(solicitud, cancellationToken);

            if (!resultado.Exito)
            {
                MensajeError = resultado.Mensaje;
                return Page();
            }

            var completado = new PagoCompletado
            {
                Cuenta = pendiente.Cuenta,
                Nombre = pendiente.Nombre,
                Importe = resultado.ImporteAplicado,
                Folio = resultado.Folio,
                IdOrden = pendiente.IdOrden,
                MarcaTarjeta = tarjeta?.Brand ?? string.Empty,
                UltimosDigitos = ultimosDigitos,
                Contingencia = resultado.Contingencia,
                Fecha = fechaPago
            };

            HttpContext.Session.SetString(
                "PagoCompletado",
                JsonSerializer.Serialize(completado));

            HttpContext.Session.Remove("PagoPendiente");

            // Comprobante informativo por correo, solo si conocemos un correo
            // del usuario en sesión. No bloquea la confirmación del pago.
            var usuarioSesion = HttpContext.Session.GetObject<objReponse>("UsuarioSesion");
            var correoPago = usuarioSesion?.CORREO?.Trim();

            if (!string.IsNullOrWhiteSpace(correoPago))
            {
                _ = _emailService.SendEmailAsync(
                    correoPago,
                    "Comprobante de pago en línea",
                    EmailTemplates.PagoComprobante(
                        completado.Nombre,
                        completado.Cuenta,
                        completado.Importe,
                        completado.Folio,
                        completado.Fecha));
            }

            return RedirectToPage("/PagoCompletado");
        }
    }
}
