using System.Net.Http.Json;
using System.Text.Json;
using AdministradorDeCuentas.Models.Constancias;
using AdministradorDeCuentas.Models.PagoEnLinea;

namespace AdministradorDeCuentas.Services
{
    public sealed class RecibosApiService
    {
        private readonly HttpClient _client;
        private readonly ILogger<RecibosApiService> _logger;

        public RecibosApiService(
            IHttpClientFactory httpClientFactory,
            ILogger<RecibosApiService> logger)
        {
            _client = httpClientFactory.CreateClient("RecibosClient");
            _logger = logger;
        }

        public async Task<AdeudoResult> ObtenerAdeudoAsync(int cuenta, CancellationToken cancellationToken)
        {
            var payload = new
            {
                alias = "ADEUDO_CUENTA",
                filtros = new[]
                {
                    new { id_cuenta = cuenta }
                }
            };

            try
            {
                using var response = await _client.PostAsJsonAsync(
                    "pagos/ObtenAdeudo",
                    payload,
                    cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "ObtenAdeudo respondió {StatusCode} para la cuenta {Cuenta}.",
                        response.StatusCode,
                        cuenta);

                    return AdeudoResult.Error(
                        ExtraerMensaje(responseBody)
                            ?? "No fue posible consultar el adeudo en este momento. Intenta de nuevo más tarde.");
                }

                var adeudos = JsonSerializer.Deserialize<List<AdeudoApiItem>>(responseBody);
                var adeudo = adeudos?.FirstOrDefault();

                if (adeudo is null)
                {
                    return new AdeudoResult
                    {
                        Exito = true,
                        CuentaValida = false,
                        Mensaje = "No se encontró información para la cuenta indicada."
                    };
                }

                if (adeudo.CuentaValida != true)
                {
                    return new AdeudoResult
                    {
                        Exito = true,
                        CuentaValida = false,
                        Mensaje = "La cuenta indicada no existe. Verifica el número e intenta de nuevo."
                    };
                }

                var nombre = NormalizarNombre(adeudo.Nombre);

                if (adeudo.TieneNegociacion == true)
                {
                    return new AdeudoResult
                    {
                        Exito = true,
                        CuentaValida = true,
                        Nombre = nombre,
                        TieneNegociacion = true,
                        Mensaje = "La cuenta tiene una negociación activa. Acude a la sucursal para más información.",
                        IdSaldo = adeudo.IdSaldo ?? 0,
                        Servidor = adeudo.Servidor ?? "comapa"
                    };
                }

                var monto = adeudo.Adeudo ?? 0m;

                return new AdeudoResult
                {
                    Exito = true,
                    CuentaValida = true,
                    Nombre = nombre,
                    Adeudo = monto,
                    TieneNegociacion = false,
                    Mensaje = monto > 0
                        ? "Consulta exitosa."
                        : "La cuenta no tiene adeudo pendiente.",
                    KeyPago = adeudo.KeyPago ?? string.Empty,
                    IdOrdenCompra = adeudo.IdOrdenCompra ?? string.Empty,
                    IdSaldo = adeudo.IdSaldo ?? 0,
                    Servidor = adeudo.Servidor ?? "comapa"
                };
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Tiempo de espera agotado al consultar el adeudo de la cuenta {Cuenta}.",
                    cuenta);

                return AdeudoResult.Error("La consulta tardó demasiado. Intenta de nuevo.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Error de comunicación al consultar el adeudo de la cuenta {Cuenta}.",
                    cuenta);

                return AdeudoResult.Error("No fue posible conectar con el servicio de pagos.");
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Respuesta inválida del servicio de pagos para la cuenta {Cuenta}.",
                    cuenta);

                return AdeudoResult.Error("El servicio de pagos devolvió una respuesta no válida.");
            }
        }

        public async Task<EstadoPagoEnLinea> ObtenerEstadoAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _client.GetAsync("pagos/estado", cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "El estado del pago en línea respondió {StatusCode}.",
                        response.StatusCode);

                    return EstadoPagoEnLinea.NoDisponible;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                return JsonSerializer.Deserialize<EstadoPagoEnLinea>(body)
                    ?? EstadoPagoEnLinea.NoDisponible;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Tiempo de espera agotado al consultar el estado del pago en línea.");
                return EstadoPagoEnLinea.NoDisponible;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No fue posible consultar el estado del pago en línea.");
                return EstadoPagoEnLinea.NoDisponible;
            }
        }

        public async Task<(bool Exito, string Mensaje)> RegistrarIntentoAsync(
            RegistrarIntentoRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _client.PostAsJsonAsync(
                    "pagos/RegistrarIntento",
                    request,
                    cancellationToken);

                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "RegistrarIntento respondió {StatusCode} para la orden {Orden}.",
                        response.StatusCode,
                        request.COrdenCompra);

                    return (false, ExtraerMensaje(body)
                        ?? "No fue posible registrar el intento de pago.");
                }

                return (true, string.Empty);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Tiempo de espera agotado al registrar el intento de la orden {Orden}.",
                    request.COrdenCompra);

                return (false, "El registro del intento de pago tardó demasiado. Intenta de nuevo.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "No fue posible registrar el intento de la orden {Orden}.",
                    request.COrdenCompra);

                return (false, "No fue posible conectar con el servicio de pagos.");
            }
        }

        public async Task<AplicarPagoResult> AplicarPagoAsync(
            AplicarPagoApiRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _client.PostAsJsonAsync(
                    "pagos/AplicarPago",
                    request,
                    cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                List<AplicarPagoApiResponse>? respuestas = null;

                try
                {
                    respuestas = JsonSerializer.Deserialize<List<AplicarPagoApiResponse>>(responseBody);
                }
                catch (JsonException)
                {
                    // Se maneja abajo con el mensaje genérico.
                }

                if (!response.IsSuccessStatusCode)
                {
                    var detalle = respuestas?.FirstOrDefault();

                    _logger.LogWarning(
                        "AplicarPago respondió {StatusCode} para la orden {Orden}. Error: {Error}.",
                        response.StatusCode,
                        request.COrdenCompra,
                        detalle?.ErrorMensaje ?? detalle?.Mensaje ?? "sin detalle");

                    return AplicarPagoResult.Error(
                        detalle?.Mensaje
                            ?? detalle?.ErrorMensaje
                            ?? ExtraerMensaje(responseBody)
                            ?? "No fue posible aplicar el pago. Intenta de nuevo o acude a la sucursal.",
                        detalle?.ErrorNum);
                }

                var exitoso = respuestas?
                    .FirstOrDefault(r => (r.ErrorNum ?? 0) == 0 && !string.IsNullOrWhiteSpace(r.Folio));

                if (exitoso is null)
                {
                    var detalle = respuestas?.FirstOrDefault();

                    _logger.LogWarning(
                        "AplicarPago no generó folio para la orden {Orden}. Error: {Error}.",
                        request.COrdenCompra,
                        detalle?.ErrorMensaje ?? detalle?.Mensaje ?? "sin detalle");

                    return AplicarPagoResult.Error(
                        detalle?.Mensaje
                            ?? detalle?.ErrorMensaje
                            ?? "El pago no generó un folio. Acude a la sucursal con tu comprobante bancario.",
                        detalle?.ErrorNum);
                }

                return new AplicarPagoResult
                {
                    Exito = true,
                    Folio = exitoso.Folio!,
                    Mensaje = exitoso.Mensaje ?? "El pago se aplicó correctamente.",
                    ImporteAplicado = exitoso.ImporteAplicado ?? request.DImporte,
                    Servidor = exitoso.Servidor
                        ?? request.Servidor
                        ?? "comapa"
                };
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Tiempo de espera agotado al aplicar el pago de la orden {Orden}.",
                    request.COrdenCompra);

                return AplicarPagoResult.Error(
                    "La aplicación del pago tardó demasiado. No vuelvas a pagar; acude a la sucursal con tu comprobante.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Error de comunicación al aplicar el pago de la orden {Orden}.",
                    request.COrdenCompra);

                return AplicarPagoResult.Error(
                    "No fue posible conectar con el sistema comercial. No vuelvas a pagar; acude a la sucursal con tu comprobante.");
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Respuesta inválida al aplicar el pago de la orden {Orden}.",
                    request.COrdenCompra);

                return AplicarPagoResult.Error(
                    "El sistema comercial devolvió una respuesta no válida. Acude a la sucursal con tu comprobante.");
            }
        }

        public async Task<ConstanciaValidacionApiResponse> ValidarConstanciaAsync(
            int cuenta,
            CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _client.PostAsJsonAsync(
                    "constancias/validar",
                    new { idCuenta = cuenta },
                    cancellationToken);

                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return new ConstanciaValidacionApiResponse
                    {
                        Exito = false,
                        Mensaje = ExtraerMensaje(body)
                            ?? "No fue posible validar la cuenta para la constancia."
                    };
                }

                return JsonSerializer.Deserialize<ConstanciaValidacionApiResponse>(body)
                    ?? new ConstanciaValidacionApiResponse
                    {
                        Exito = false,
                        Mensaje = "La API devolvió una respuesta no válida."
                    };
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(ex, "Error validando la constancia de la cuenta {Cuenta}.", cuenta);

                return new ConstanciaValidacionApiResponse
                {
                    Exito = false,
                    Mensaje = "No fue posible conectar con el sistema comercial. Intenta de nuevo."
                };
            }
        }

        public async Task<ConstanciaSolicitudApiResponse> SolicitarConstanciaAsync(
            ConstanciaSolicitudApiRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _client.PostAsJsonAsync(
                    "constancias/solicitar",
                    request,
                    cancellationToken);

                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return new ConstanciaSolicitudApiResponse
                    {
                        Exito = false,
                        Mensaje = ExtraerMensaje(body)
                            ?? "No fue posible registrar la solicitud de constancia."
                    };
                }

                return JsonSerializer.Deserialize<ConstanciaSolicitudApiResponse>(body)
                    ?? new ConstanciaSolicitudApiResponse
                    {
                        Exito = false,
                        Mensaje = "La API devolvió una respuesta no válida."
                    };
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Error registrando la constancia de la cuenta {Cuenta}.",
                    request.IdCuenta);

                return new ConstanciaSolicitudApiResponse
                {
                    Exito = false,
                    Mensaje = "No fue posible conectar con el sistema comercial. Intenta de nuevo."
                };
            }
        }

        public async Task<ConstanciaPagoPreviewApiResponse> ObtenerPreviewPagoConstanciaAsync(
            long idConstancia,
            int idCuenta,
            CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _client.PostAsJsonAsync(
                    "constancias/preview-pago",
                    new { idConstancia, idCuenta },
                    cancellationToken);

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var resultado = JsonSerializer.Deserialize<ConstanciaPagoPreviewApiResponse>(body);

                if (resultado is null)
                {
                    return new ConstanciaPagoPreviewApiResponse
                    {
                        Exito = false,
                        Mensaje = "La API devolvió una respuesta no válida."
                    };
                }

                if (!response.IsSuccessStatusCode)
                {
                    resultado.Exito = false;
                }

                return resultado;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Error consultando el cobro de la constancia {Constancia}.",
                    idConstancia);

                return new ConstanciaPagoPreviewApiResponse
                {
                    Exito = false,
                    Mensaje = "No fue posible consultar el cobro de la constancia."
                };
            }
        }

        public async Task<byte[]?> ObtenerPdfConstanciaAsync(
            long idConstancia,
            CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _client.GetAsync(
                    $"constancias/{idConstancia}/pdf",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);

                    _logger.LogWarning(
                        "La constancia {Constancia} no se pudo descargar ({StatusCode}): {Mensaje}",
                        idConstancia,
                        response.StatusCode,
                        ExtraerMensaje(body));

                    return null;
                }

                return await response.Content.ReadAsByteArrayAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Error descargando el PDF de la constancia {Constancia}.",
                    idConstancia);

                return null;
            }
        }

        public async Task<ConstanciaPagoApiResponse> AplicarPagoConstanciaAsync(
            ConstanciaPagoApiRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _client.PostAsJsonAsync(
                    "constancias/aplicar-pago",
                    request,
                    cancellationToken);

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var resultado = JsonSerializer.Deserialize<ConstanciaPagoApiResponse>(body);

                if (resultado is null)
                {
                    return new ConstanciaPagoApiResponse
                    {
                        Exito = false,
                        Mensaje = "La API devolvió una respuesta no válida."
                    };
                }

                if (!response.IsSuccessStatusCode)
                {
                    resultado.Exito = false;
                }

                return resultado;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Error aplicando el pago de la constancia {Constancia}.",
                    request.IdConstancia);

                return new ConstanciaPagoApiResponse
                {
                    Exito = false,
                    Mensaje = "No fue posible aplicar el pago de la constancia."
                };
            }
        }

        private static string? ExtraerMensaje(string? body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            try
            {
                using var documento = JsonDocument.Parse(body);
                var raiz = documento.RootElement;

                if (raiz.ValueKind == JsonValueKind.Object)
                {
                    if (raiz.TryGetProperty("message", out var message) &&
                        message.ValueKind == JsonValueKind.String)
                    {
                        return message.GetString();
                    }

                    if (raiz.TryGetProperty("mensaje", out var mensaje) &&
                        mensaje.ValueKind == JsonValueKind.String)
                    {
                        return mensaje.GetString();
                    }
                }

                if (raiz.ValueKind == JsonValueKind.Array && raiz.GetArrayLength() > 0)
                {
                    var primero = raiz[0];

                    if (primero.TryGetProperty("error_mensaje", out var error) &&
                        error.ValueKind == JsonValueKind.String)
                    {
                        return error.GetString();
                    }

                    if (primero.TryGetProperty("mensaje", out var mensaje) &&
                        mensaje.ValueKind == JsonValueKind.String)
                    {
                        return mensaje.GetString();
                    }
                }
            }
            catch (JsonException)
            {
                // El cuerpo no es JSON; se usa el mensaje genérico.
            }

            return null;
        }

        private static string NormalizarNombre(string? nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return string.Empty;
            }

            var partes = nombre.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(' ', partes);
        }
    }
}
