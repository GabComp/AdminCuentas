using System.Text.Json;
using AdministradorDeCuentas.Models.Constancias;
using AdministradorDeCuentas.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministradorDeCuentas.Pages
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class PagoConstanciaModel : PageModel
    {
        private readonly RecibosApiService _recibos;
        private readonly ILogger<PagoConstanciaModel> _logger;

        public PagoConstanciaModel(
            RecibosApiService recibos,
            ILogger<PagoConstanciaModel> logger)
        {
            _recibos = recibos;
            _logger = logger;
        }

        public ConstanciaPagoPreviewApiResponse? Pago { get; private set; }

        public string? MensajeError { get; private set; }

        public async Task<IActionResult> OnGetAsync(
            long idConstancia,
            int idCuenta,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("ID_USUARIO")))
            {
                return Redirect("/Home/Index");
            }

            if (idConstancia <= 0 || idCuenta <= 0 || !CuentaPerteneceAlUsuario(idCuenta))
            {
                MensajeError = "No tienes acceso a esa constancia.";
                return Page();
            }

            var preview = await _recibos.ObtenerPreviewPagoConstanciaAsync(
                idConstancia,
                idCuenta,
                cancellationToken);

            if (!preview.Exito)
            {
                MensajeError = preview.Mensaje;
                return Page();
            }

            Pago = preview;

            if (!preview.PuedeCobrar)
            {
                MensajeError = string.IsNullOrWhiteSpace(preview.Mensaje)
                    ? "La constancia no se encuentra disponible para pago."
                    : preview.Mensaje;
            }

            return Page();
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
