//using Microsoft.AspNetCore.Mvc.RazorPages;
//using AdministradorDeCuentas.Services;
////using PrototipoWebSitioComapa.Services;

//namespace AdministradorDeCuentas.Pages
//{
//    public class PagoReciboModel : PageModel
//    {
//        private readonly IConfiguration _configuration;
//        private readonly RecibosApiService _recibos;

//        public PagoReciboModel(
//            IConfiguration configuration,
//            RecibosApiService recibos)
//        {
//            _configuration = configuration;
//            _recibos = recibos;
//        }

//        public bool PagoHabilitado { get; private set; }

//        public string MensajePago { get; private set; } =
//            "El pago en línea estará disponible próximamente.";

//        public async Task OnGetAsync(CancellationToken cancellationToken)
//        {
//            var permitido = _configuration.GetValue<bool>("ApiSettings:PermitirAplicarPago");
//            var estado = await _recibos.ObtenerEstadoAsync(cancellationToken);

//            PagoHabilitado = permitido && !estado.Suspendido;

//            if (estado.Suspendido)
//            {
//                MensajePago = string.IsNullOrWhiteSpace(estado.Mensaje)
//                    ? "Por favor intente más tarde."
//                    : estado.Mensaje;
//            }
//        }
//    }
//}
