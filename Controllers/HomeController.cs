using AdministradorDeCuentas.Filters;
using AdministradorDeCuentas.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AdministradorDeCuentas.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
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
