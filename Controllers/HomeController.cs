using AdministradorDeCuentas.Filters;
using AdministradorDeCuentas.Models;
using AdministradorDeCuentas.Models.ReporteFuga;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace AdministradorDeCuentas.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Inicio()
        {
            return View();
        }

        public IActionResult Faq()
        {
            return View();
        }

        public IActionResult ReporteFuga()
        {
            return View();
        }

        [HttpPost]
        [EnableRateLimiting("reporte-fuga")]
        public async Task<JsonResult> ReporteFuga([FromBody] ReporteFugaRequest? reporte)
        {
            if (reporte is null)
            {
                return Json(new { exito = false, mensaje = "Solicitud inválida." });
            }

            reporte.Nombre = reporte.Nombre?.Trim() ?? string.Empty;
            reporte.TipoReporte = reporte.TipoReporte?.Trim() ?? string.Empty;
            reporte.Descripcion = reporte.Descripcion?.Trim() ?? string.Empty;
            reporte.Telefono = reporte.Telefono?.Trim() ?? string.Empty;
            reporte.Correo = reporte.Correo?.Trim() ?? string.Empty;
            reporte.Direccion = reporte.Direccion?.Trim() ?? string.Empty;
            reporte.Colonia = reporte.Colonia?.Trim() ?? string.Empty;
            reporte.Referencia = reporte.Referencia?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(reporte.Nombre) || reporte.Nombre.Length > 100)
                return Json(new { exito = false, mensaje = "Indica tu nombre." });

            if (reporte.TipoReporte is not ("FUGA" or "DESPERFECTO"))
                return Json(new { exito = false, mensaje = "Selecciona el tipo de reporte." });

            if (string.IsNullOrWhiteSpace(reporte.Descripcion) || reporte.Descripcion.Length > 1000)
                return Json(new { exito = false, mensaje = "Describe el problema." });

            if (!string.IsNullOrWhiteSpace(reporte.Telefono) && !Regex.IsMatch(reporte.Telefono, @"^\d{10}$"))
                return Json(new { exito = false, mensaje = "El teléfono debe contener 10 dígitos." });

            if (!string.IsNullOrWhiteSpace(reporte.Correo) &&
                (reporte.Correo.Length > 254 || !Regex.IsMatch(reporte.Correo, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")))
                return Json(new { exito = false, mensaje = "El correo no tiene un formato válido." });

            try
            {
                var client = _httpClientFactory.CreateClient("RecibosClient");
                using var response = await client.PostAsJsonAsync("reportes/fuga", reporte);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "La API de reportes respondió {StatusCode} al registrar una fuga.",
                        (int)response.StatusCode);

                    return Json(new { exito = false, mensaje = "No fue posible enviar el reporte. Intenta de nuevo." });
                }

                return Json(new { exito = true, mensaje = "Reporte enviado correctamente. Gracias por avisar." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar el reporte de fuga.");
                return Json(new { exito = false, mensaje = "No fue posible enviar el reporte. Intenta de nuevo." });
            }
        }

        [ValidarSesion]
        public IActionResult Menu()
        {
            return View();
        }

        public IActionResult Registro()
        {
            return View();
        }

        public IActionResult RestableceContra()
        {
            return View();
        }
        [ValidarSesion]
        public IActionResult ActualizarDatos()
        {
            return View();
        }
        
        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult CambioContrasenia()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
 
    }
}
