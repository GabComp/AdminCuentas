using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AdministradorDeCuentas.Models.PagoEnLinea;

namespace AdministradorDeCuentas.Pages
{
    public class PagoCompletadoModel : PageModel
    {
        public PagoCompletado? Pago { get; private set; }

        public IActionResult OnGet()
        {
            var pagoJson = HttpContext.Session.GetString("PagoCompletado");

            if (string.IsNullOrWhiteSpace(pagoJson))
            {
                return RedirectToPage("/pago-en-linea");
            }

            try
            {
                Pago = JsonSerializer.Deserialize<PagoCompletado>(pagoJson);
            }
            catch (JsonException)
            {
                return RedirectToPage("/pago-en-linea");
            }

            if (Pago is null)
            {
                return RedirectToPage("/pago-en-linea");
            }

            return Page();
        }
    }
}
