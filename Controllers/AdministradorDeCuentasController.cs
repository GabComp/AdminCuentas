using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AdministradorDeCuentas.Filters;
using AdministradorDeCuentas.Models.APIRecibos;
using AdministradorDeCuentas.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using static AdministradorDeCuentas.Models.API.Transactions.CunetaUsuariosTransaction;
using static APIPipas.Models.CuentaUsuarios.CuentaUsuarios;
using static PrototipoComapa.Models.API.Transactions.UsuarioWebTransactions;

namespace AdministradorDeCuentas.Controllers
{
    [ValidarSesion]
    public class AdministradorDeCuentasController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly RecibosAuthService _auth;
        private readonly ILogger<AdministradorDeCuentasController> _logger;

        public AdministradorDeCuentasController(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            RecibosAuthService auth,
            ILogger<AdministradorDeCuentasController> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _auth = auth;
            _logger = logger;
        }

        public IActionResult AdministradorDeCuentas()
        {
            return View();
        }

        [HttpPost]
        [EnableRateLimiting("recibos")]
        public async Task<IActionResult> ValidarCuentasUsuarios([FromBody] DatosValidacion? Jcontent)
        {
            var filtro = Jcontent?.filtros?.FirstOrDefault();

            if (filtro?.id_cuenta is null or <= 0 ||
                string.IsNullOrWhiteSpace(filtro.nombre) ||
                filtro.total is null or <= 0)
            {
                return Json(new
                {
                    ok = false,
                    message = "Debe indicar cuenta, nombre y total a validar."
                });
            }

            var cancellationToken = HttpContext.RequestAborted;

            try
            {
                var valida = await ValidarCuentaAsync(
                    filtro.id_cuenta.Value,
                    filtro.nombre,
                    filtro.total.Value,
                    cancellationToken);

                return Json(new { ok = true, body = valida });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Configuración incompleta al validar la cuenta.");
                return ServicioNoDisponible();
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Tiempo de espera agotado al validar la cuenta {Cuenta}.", filtro.id_cuenta);
                return StatusCode(StatusCodes.Status504GatewayTimeout, new
                {
                    ok = false,
                    message = "La validación tardó demasiado. Intenta de nuevo."
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de comunicación al validar la cuenta {Cuenta}.", filtro.id_cuenta);
                return ServicioNoDisponible();
            }
        }

        [HttpPost]
        [EnableRateLimiting("recibos")]
        public async Task<JsonResult> ConsultarCuentasUsuario([FromBody] USUARIO_WEB_CUENTA? request)
        {
            if (request is null)
            {
                return Json(new CuentasUsuarioResponse
                {
                    Exito = false,
                    Mensaje = "Solicitud inválida.",
                    Datos = new List<USUARIO_WEB_CUENTA_Result>()
                });
            }

            if (!TryObtenerIdUsuarioSesion(out var idUsuarioWeb))
            {
                return Json(new CuentasUsuarioResponse
                {
                    Exito = false,
                    Mensaje = "La sesión expiró. Inicia sesión nuevamente.",
                    Datos = new List<USUARIO_WEB_CUENTA_Result>()
                });
            }

            request.ID_USUARIO_WEB = idUsuarioWeb;

            try
            {
                var apiRequest = new CuentaUsuarioApiRequest
                {
                    Application = "CuentaUsuarios",
                    TransactionList = new List<CuentaUsuarioTransactionRequest>
                    {
                        new CuentaUsuarioTransactionRequest
                        {
                            TransactionName = "ConsultaCuentaUsuario",
                            TransactionError = "",
                            TransactionLog = true,
                            TransactionAttributes = new USUARIO_WEB_CUENTA
                            {
                                ID_USUARIO_WEB = idUsuarioWeb
                            }
                        }
                    }
                };

                var client = _httpClientFactory.CreateClient("ApiGeneralClient");
                var json = JsonSerializer.Serialize(apiRequest);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = await client.PostAsync("API/Transaction", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "La API de cuentas respondió {StatusCode} al consultar.",
                        (int)response.StatusCode);

                    return Json(new CuentasUsuarioResponse
                    {
                        Exito = false,
                        Mensaje = "No fue posible consultar tus cuentas. Intenta de nuevo.",
                        Datos = new List<USUARIO_WEB_CUENTA_Result>()
                    });
                }

                var apiResponse = JsonSerializer.Deserialize<ApiCunetaUsuariosResponse>(responseContent);

                if (apiResponse is null || apiResponse.ResponseCode != 1)
                {
                    _logger.LogWarning(
                        "La API de cuentas rechazó la consulta: {Mensaje}.",
                        apiResponse?.ResponseMessage);

                    return Json(new CuentasUsuarioResponse
                    {
                        Exito = false,
                        Mensaje = "No fue posible consultar tus cuentas. Intenta de nuevo.",
                        Datos = new List<USUARIO_WEB_CUENTA_Result>()
                    });
                }

                if (apiResponse.TransactionResponses is null ||
                    apiResponse.TransactionResponses.Count == 0)
                {
                    return Json(new CuentasUsuarioResponse
                    {
                        Exito = true,
                        Datos = new List<USUARIO_WEB_CUENTA_Result>(),
                        Mensaje = "No tienes cuentas registradas."
                    });
                }

                var transaction = apiResponse.TransactionResponses[0];

                if (transaction.TransactionStatus != 1)
                {
                    _logger.LogWarning(
                        "La consulta de cuentas falló: {Mensaje}.",
                        transaction.TransactionMessage);

                    return Json(new CuentasUsuarioResponse
                    {
                        Exito = false,
                        Mensaje = "No fue posible consultar tus cuentas. Intenta de nuevo.",
                        Datos = new List<USUARIO_WEB_CUENTA_Result>()
                    });
                }

                var datos = transaction.TransactionResults ?? new List<USUARIO_WEB_CUENTA_Result>();
                var cuentas = datos
                    .Select(x => new { x.CUENTA_COMAPA, x.ALIAS_CUENTA })
                    .ToList();

                // La lista se guarda en sesión solo después de una consulta exitosa.
                HttpContext.Session.SetString("CuentasUsuario", JsonSerializer.Serialize(cuentas));

                return Json(new CuentasUsuarioResponse
                {
                    Exito = true,
                    Datos = datos,
                    Mensaje = "Consulta exitosa"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al consultar las cuentas del usuario.");
                return Json(new CuentasUsuarioResponse
                {
                    Exito = false,
                    Mensaje = "No fue posible consultar tus cuentas. Intenta de nuevo.",
                    Datos = new List<USUARIO_WEB_CUENTA_Result>()
                });
            }
        }

        [HttpPost]
        [EnableRateLimiting("recibos")]
        public async Task<IActionResult> ActionsCuentaUsuario([FromBody] ACTIONS_USUARIO_WEB_CUENTA? request)
        {
            if (request is null)
            {
                return Json(new ActionsCuentasResponse
                {
                    Exito = false,
                    Mensaje = "Solicitud inválida.",
                    Datos = new List<ACTIONS_USUARIO_WEB_CUENTAResult>()
                });
            }

            if (!TryObtenerIdUsuarioSesion(out var idUsuarioWeb))
            {
                return Json(new ActionsCuentasResponse
                {
                    Exito = false,
                    Mensaje = "La sesión expiró. Inicia sesión nuevamente.",
                    Datos = new List<ACTIONS_USUARIO_WEB_CUENTAResult>()
                });
            }

            var accion = (request.ACCION ?? string.Empty).Trim().ToUpperInvariant();

            if (accion is not ("ALTA" or "BAJA"))
            {
                return Json(new ActionsCuentasResponse
                {
                    Exito = false,
                    Mensaje = "Acción no permitida.",
                    Datos = new List<ACTIONS_USUARIO_WEB_CUENTAResult>()
                });
            }

            if (request.CUENTA_COMAPA <= 0)
            {
                return Json(new ActionsCuentasResponse
                {
                    Exito = false,
                    Mensaje = "Debe indicar una cuenta válida.",
                    Datos = new List<ACTIONS_USUARIO_WEB_CUENTAResult>()
                });
            }

            var cancellationToken = HttpContext.RequestAborted;

            try
            {
                if (accion == "ALTA")
                {
                    // No se confía en que el navegador haya llamado antes a ValidarCuentasUsuarios.
                    var valida = await ValidarCuentaAsync(
                        request.CUENTA_COMAPA,
                        request.Nombre_Propietario ?? string.Empty,
                        request.PAGO_TOTAL,
                        cancellationToken);

                    if (valida.Count == 0 || valida[0].usrValido != true)
                    {
                        _logger.LogWarning(
                            "El usuario intentó vincular la cuenta {Cuenta} sin una validación exitosa.",
                            request.CUENTA_COMAPA);

                        return StatusCode(StatusCodes.Status403Forbidden, new ActionsCuentasResponse
                        {
                            Exito = false,
                            Mensaje = "No fue posible validar la cuenta con los datos proporcionados.",
                            Datos = new List<ACTIONS_USUARIO_WEB_CUENTAResult>()
                        });
                    }
                }
                else if (!CuentaPerteneceAlUsuario(request.CUENTA_COMAPA))
                {
                    _logger.LogWarning(
                        "El usuario intentó dar de baja una cuenta que no le pertenece ({Cuenta}).",
                        request.CUENTA_COMAPA);

                    return StatusCode(StatusCodes.Status403Forbidden, new ActionsCuentasResponse
                    {
                        Exito = false,
                        Mensaje = "No tienes acceso a esa cuenta.",
                        Datos = new List<ACTIONS_USUARIO_WEB_CUENTAResult>()
                    });
                }

                request.ID_USUARIO_WEB = idUsuarioWeb;
                request.ACCION = accion;

                var apiRequest = new ActionsApiRequest
                {
                    Application = "CuentaUsuarios",
                    TransactionList = new List<ActionsCuentaTransactionRequest>
                    {
                        new ActionsCuentaTransactionRequest
                        {
                            TransactionName = "ActionsCuentaUsuarios",
                            TransactionError = "",
                            TransactionLog = true,
                            TransactionAttributes = new ACTIONS_USUARIO_WEB_CUENTA
                            {
                                ACCION = accion,
                                ID_USUARIO_WEB = idUsuarioWeb,
                                CUENTA_COMAPA = request.CUENTA_COMAPA,
                                MOTIVO_BAJA = request.MOTIVO_BAJA ?? string.Empty,
                                ID_IMAGEN_AUTORIZA_PAPERLESS = request.ID_IMAGEN_AUTORIZA_PAPERLESS,
                                ALIAS_CUENTA = request.ALIAS_CUENTA ?? string.Empty,
                                Nombre_Propietario = request.Nombre_Propietario ?? string.Empty,
                                PAGO_TOTAL = request.PAGO_TOTAL
                            }
                        }
                    }
                };

                var client = _httpClientFactory.CreateClient("ApiGeneralClient");
                var json = JsonSerializer.Serialize(apiRequest);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = await client.PostAsync("API/Transaction", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "La API de cuentas respondió {StatusCode} en la acción {Accion}.",
                        (int)response.StatusCode,
                        accion);

                    return Json(new ActionsCuentasResponse
                    {
                        Exito = false,
                        Mensaje = "No fue posible completar la operación. Intenta de nuevo.",
                        Datos = new List<ACTIONS_USUARIO_WEB_CUENTAResult>()
                    });
                }

                var apiResponse = JsonSerializer.Deserialize<ApiActionsCunetaResponse>(responseContent);

                if (apiResponse is null || apiResponse.ResponseCode != 1)
                {
                    _logger.LogWarning(
                        "La API de cuentas rechazó la acción {Accion}: {Mensaje}.",
                        accion,
                        apiResponse?.ResponseMessage);

                    return Json(new ActionsCuentasResponse
                    {
                        Exito = false,
                        Mensaje = "No fue posible completar la operación. Intenta de nuevo.",
                        Datos = new List<ACTIONS_USUARIO_WEB_CUENTAResult>()
                    });
                }

                if (apiResponse.TransactionResponses is null ||
                    apiResponse.TransactionResponses.Count == 0)
                {
                    return Json(new ActionsCuentasResponse
                    {
                        Exito = false,
                        Mensaje = "No fue posible completar la operación. Intenta de nuevo.",
                        Datos = new List<ACTIONS_USUARIO_WEB_CUENTAResult>()
                    });
                }

                var transaction = apiResponse.TransactionResponses[0];

                if (transaction.TransactionStatus != 1)
                {
                    _logger.LogWarning(
                        "La acción {Accion} falló: {Mensaje}.",
                        accion,
                        transaction.TransactionMessage);

                    return Json(new ActionsCuentasResponse
                    {
                        Exito = false,
                        Mensaje = transaction.TransactionMessage ?? "No fue posible completar la operación.",
                        Datos = new List<ACTIONS_USUARIO_WEB_CUENTAResult>()
                    });
                }

                return Json(new ActionsCuentasResponse
                {
                    Exito = true,
                    Datos = transaction.TransactionResults ?? new List<ACTIONS_USUARIO_WEB_CUENTAResult>(),
                    Mensaje = "Operación exitosa"
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Configuración incompleta en la acción {Accion}.", accion);
                return ServicioNoDisponible();
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Tiempo de espera agotado en la acción {Accion}.", accion);
                return StatusCode(StatusCodes.Status504GatewayTimeout, new ActionsCuentasResponse
                {
                    Exito = false,
                    Mensaje = "La operación tardó demasiado. Intenta de nuevo.",
                    Datos = new List<ACTIONS_USUARIO_WEB_CUENTAResult>()
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de comunicación en la acción {Accion}.", accion);
                return ServicioNoDisponible();
            }
        }

        private bool TryObtenerIdUsuarioSesion(out int idUsuarioWeb)
        {
            idUsuarioWeb = 0;
            var idUsuario = HttpContext.Session.GetString("ID_USUARIO");

            return int.TryParse(idUsuario, out idUsuarioWeb) && idUsuarioWeb > 0;
        }

        private async Task<List<DatosValidacionResponse>> ValidarCuentaAsync(
            int idCuenta,
            string nombre,
            decimal total,
            CancellationToken cancellationToken)
        {
            var payload = new DatosValidacion
            {
                alias = "VALIDA_CUENTA",
                filtros = new List<Filtro>
                {
                    new()
                    {
                        id_cuenta = idCuenta,
                        nombre = nombre,
                        total = total
                    }
                }
            };

            using var response = await EnviarRecibosAsync(
                "comercial/ValidaCuenta",
                payload,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ValidaCuenta respondió {StatusCode} para la cuenta {Cuenta}.",
                    (int)response.StatusCode,
                    idCuenta);

                return new List<DatosValidacionResponse>();
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            return JsonSerializer.Deserialize<List<DatosValidacionResponse>>(
                       body,
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? new List<DatosValidacionResponse>();
        }

        private async Task<HttpResponseMessage> EnviarRecibosAsync(
            string ruta,
            object payload,
            CancellationToken cancellationToken)
        {
            var token = await _auth.ObtenerTokenAsync(cancellationToken);
            var client = _httpClientFactory.CreateClient("RecibosClient");

            var request = new HttpRequestMessage(HttpMethod.Post, ruta)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return await client.SendAsync(request, cancellationToken);
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
                var cuentas = JsonSerializer.Deserialize<List<CuentaSesion>>(
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

        private ObjectResult ServicioNoDisponible() =>
            StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                exito = false,
                mensaje = "El servicio de cuentas no está disponible en este momento."
            });

        public sealed class CuentaSesion
        {
            public int CUENTA_COMAPA { get; set; }
        }
    }
}


