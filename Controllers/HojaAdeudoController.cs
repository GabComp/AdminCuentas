using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AdministradorDeCuentas.Controllers
{
    public class HojaAdeudoController : Controller
    {
        // GET: HojaAdeudoController
        public ActionResult HojaAdeudo()
        {
            return View();
        }

    }
}
