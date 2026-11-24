using System.Text.Json;
using AdministradorDeCuentas.Filters;
using AdministradorDeCuentas.Models.Constancias;
using AdministradorDeCuentas.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PrototipoComapa.Extensions;
using PrototipoComapa.Models.UsuarioWeb;
using PrototipoComapa.Utilerias;

namespace AdministradorDeCuentas.Controllers
{
    public class HojaAdeudoController : Controller
    {
        private readonly RecibosApiService _recibos;
        private readonly ILogger<HojaAdeudoController> _logger;

        public HojaAdeudoController(
            RecibosApiService recibos,
            ILogger<HojaAdeudoController> logger)
        {
            _recibos = recibos;
            _logger = logger;
        }

        public ActionResult HojaAdeudo()
        {
            return View();
        }

        [HttpPost]
        [ValidarSesion]
        [EnableRateLimiting("constancias")]
        public async Task<IActionResult> ValidarConstancia(
            [FromBody] ConstanciaCuentaRequest? request,
            CancellationToken cancellationToken)
        {
            if (request is null || request.IdCuenta <= 0)
            {
                return Json(new { exito = false, mensaje = "Selecciona una cuenta válida." });
            }

            if (!CuentaPerteneceAlUsuario(request.IdCuenta))
            {
                _logger.LogWarning(
                    "El usuario intentó validar una constancia de una cuenta ajena ({Cuenta}).",
                    request.IdCuenta);

                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    exito = false,
                    mensaje = "No tienes acceso a esa cuenta."
                });
            }

            var resultado = await _recibos.ValidarConstanciaAsync(request.IdCuenta, cancellationToken);

            return Json(new
            {
                exito = resultado.Exito,
                puedeRegistrar = resultado.PuedeRegistrar,
                nombre = resultado.Nombre,
                telefono = resultado.Telefono,
                adeudo = resultado.Adeudo,
                mensaje = resultado.Mensaje
            });
        }

        [HttpPost]
        [ValidarSesion]
        [EnableRateLimiting("constancias")]
        public async Task<IActionResult> SolicitarConstancia(
            [FromBody] ConstanciaSolicitudApiRequest? request,
            CancellationToken cancellationToken)
        {
            if (request is null || request.IdCuenta <= 0)
            {
                return Json(new { exito = false, mensaje = "Solicitud inválida." });
            }

            if (!CuentaPerteneceAlUsuario(request.IdCuenta))
            {
                _logger.LogWarning(
                    "El usuario intentó solicitar una constancia de una cuenta ajena ({Cuenta}).",
                    request.IdCuenta);

                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    exito = false,
                    mensaje = "No tienes acceso a esa cuenta."
                });
            }

            var usuario = HttpContext.Session.GetObject<UsuarioWebModel.objReponse>("UsuarioSesion");
            request.UsuarioPortal = (usuario?.NOMBRE ?? string.Empty).Trim();
            request.Solicitante = request.Solicitante.Trim();
            request.Telefono = request.Telefono.Trim();
            request.Notaria = request.Notaria.Trim();
            request.DatoNotaria = request.DatoNotaria.Trim();
            request.Observacion = request.Observacion.Trim();

            var resultado = await _recibos.SolicitarConstanciaAsync(request, cancellationToken);

            return Json(new
            {
                exito = resultado.Exito,
                mensaje = resultado.Mensaje,
                folio = resultado.Folio,
                idConstancia = resultado.IdConstancia
            });
        }

        [HttpGet]
        [ValidarSesion]
        public async Task<IActionResult> DescargarConstancia(
            long idConstancia,
            int idCuenta,
            CancellationToken cancellationToken)
        {
            if (idConstancia <= 0 || idCuenta <= 0 || !CuentaPerteneceAlUsuario(idCuenta))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            var pdf = await _recibos.ObtenerPdfConstanciaAsync(idConstancia, cancellationToken);

            if (pdf is null || pdf.Length == 0)
            {
                TempData["ErrorConstancia"] =
                    "No fue posible descargar la constancia. Si ya la pagaste, intenta de nuevo en unos minutos.";

                return RedirectToAction(nameof(HojaAdeudo));
            }

            Response.Headers.CacheControl = "private, no-store";

            return File(pdf, "application/pdf", $"Constancia-{idConstancia}.pdf");
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
                var cuentas = JsonSerializer.Deserialize<List<CuentaSesionItem>>(
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

        private sealed class CuentaSesionItem
        {
            public int CUENTA_COMAPA { get; set; }
        }
    }
}
