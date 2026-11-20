using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;

namespace AdministradorDeCuentas.Services
{
    /// <summary>
    /// Obtiene y cachea el JWT de servicio de ComapaInternet.Api.
    /// Las credenciales viven únicamente en el servidor (user-secrets / variables
    /// de entorno / Key Vault). El navegador nunca las recibe.
    /// </summary>
    public sealed class RecibosAuthService
    {
        private const string ClaveCache = "recibos:token-servicio";
        private static readonly TimeSpan DuracionToken = TimeSpan.FromMinutes(25);

        private readonly HttpClient _client;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RecibosAuthService> _logger;
        private readonly SemaphoreSlim _candado = new(1, 1);

        public RecibosAuthService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IMemoryCache cache,
            ILogger<RecibosAuthService> logger)
        {
            _client = httpClientFactory.CreateClient("RecibosClient");
            _configuration = configuration;
            _cache = cache;
            _logger = logger;
        }

        public async Task<string> ObtenerTokenAsync(CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(ClaveCache, out string? tokenCacheado) &&
                !string.IsNullOrWhiteSpace(tokenCacheado))
            {
                return tokenCacheado;
            }

            await _candado.WaitAsync(cancellationToken);

            try
            {
                if (_cache.TryGetValue(ClaveCache, out tokenCacheado) &&
                    !string.IsNullOrWhiteSpace(tokenCacheado))
                {
                    return tokenCacheado;
                }

                var usuario = _configuration["ApiSettings:RecibosLoginUser"]?.Trim();
                var password = _configuration["ApiSettings:RecibosLoginPassword"]?.Trim();

                if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
                {
                    throw new InvalidOperationException(
                        "No están configuradas las credenciales de servicio de la API de recibos " +
                        "('ApiSettings:RecibosLoginUser' y 'ApiSettings:RecibosLoginPassword').");
                }

                using var response = await _client.PostAsJsonAsync(
                    "auth/login",
                    new { username = usuario, password },
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "La API de recibos rechazó el inicio de sesión de servicio con estado {StatusCode}.",
                        (int)response.StatusCode);

                    throw new InvalidOperationException(
                        "No fue posible autenticar el servicio de recibos.");
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var login = JsonSerializer.Deserialize<LoginResponse>(
                    body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (login is null || string.IsNullOrWhiteSpace(login.Token))
                {
                    _logger.LogError("La API de recibos devolvió una respuesta de login sin token.");
                    throw new InvalidOperationException(
                        "No fue posible autenticar el servicio de recibos.");
                }

                _cache.Set(ClaveCache, login.Token, DuracionToken);

                return login.Token;
            }
            finally
            {
                _candado.Release();
            }
        }

        public void InvalidarToken()
        {
            _cache.Remove(ClaveCache);
        }

        public sealed class LoginResponse
        {
            [JsonPropertyName("token")]
            public string? Token { get; set; }
        }
    }
}
