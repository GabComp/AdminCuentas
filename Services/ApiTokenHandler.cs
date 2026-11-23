using System.Net.Http.Headers;

namespace AdministradorDeCuentas.Services
{
    /// <summary>
    /// Adjunta el JWT de servicio a las llamadas a ComapaInternet.Api.
    /// El login de servicio y el health check se omiten para evitar recursión
    /// y no solicitar token en cada probe de monitoreo.
    /// </summary>
    public sealed class ApiTokenHandler : DelegatingHandler
    {
        private readonly IServiceProvider _services;

        public ApiTokenHandler(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var ruta = request.RequestUri?.AbsolutePath.TrimStart('/') ?? string.Empty;

            // El BaseAddress de los clientes incluye /api/; se normaliza para
            // reconocer las rutas que no deben llevar token.
            if (ruta.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
            {
                ruta = ruta[4..];
            }

            var omitir =
                ruta.StartsWith("auth/", StringComparison.OrdinalIgnoreCase) ||
                ruta.Equals("health", StringComparison.OrdinalIgnoreCase) ||
                ruta.StartsWith("health/", StringComparison.OrdinalIgnoreCase);

            if (!omitir)
            {
                var auth = _services.GetRequiredService<RecibosAuthService>();
                var token = await auth.ObtenerTokenAsync(cancellationToken);

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
