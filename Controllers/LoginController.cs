using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using PrototipoComapa.Extensions;
using PrototipoComapa.Utilerias;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static PrototipoComapa.Models.API.Transactions.UsuarioWebTransactions;
using static PrototipoComapa.Models.UsuarioWeb.UsuarioWebModel;

namespace PrototipoComapa.Controllers
{
    public class LoginController : Controller
    {
        private const string PrefijoOtpRegistro = "otp-registro:";
        private const int OtpTtlMinutos = 10;
        private const int OtpMaxIntentos = 5;

        private sealed record OtpRegistro(string Codigo, DateTime Expira, int Intentos);

        private readonly HttpClient _ApiClient;
        private readonly IConfiguration _configuration;
        //private string ENDPOINT_API = "https://financiero.comapavictoria.gob.mx/";
        //private string ENDPOINT_API = "http://192.168.1.70:62863/api";
        private readonly ILogger<LoginController> _logger;
        private readonly EmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly IHostEnvironment _entorno;

        public LoginController(
            ILogger<LoginController> logger,
            IHttpClientFactory factory,
            IConfiguration configuration,
            EmailService emailService,
            IMemoryCache cache,
            IHostEnvironment entorno)
        {
            _logger = logger;
            _configuration = configuration;
            _emailService = emailService;
            _cache = cache;
            _entorno = entorno;
            _ApiClient = factory.CreateClient("ApiGeneralClient");
        }

        [HttpPost]
        [EnableRateLimiting("login")]
        public async Task<JsonResult> InicioSesion([FromBody] UsuarioWebRegistroRequest? request)
        {
            if (request is null ||
                string.IsNullOrWhiteSpace(request.USUARIO) ||
                string.IsNullOrWhiteSpace(request.CONTRASENIA))
            {
                return ErrorGenerico("Usuario y contraseña son requeridos.");
            }

            try
            {
                var apiRequest = new ApiRequest
                {
                    Application = "LoginServicios",
                    TransactionList = new List<TransactionRequest>
                    {
                        new TransactionRequest
                        {
                            TransactionName = "UsuarioWeb",
                            TransactionError = "",
                            TransactionLog = true,
                            TransactionAttributes = new UsuarioWebRegistroRequest
                            {
                                // El cliente no decide la acción: este endpoint solo autentica.
                                ACCION = "ACCE",
                                USUARIO = request.USUARIO.Trim(),
                                CONTRASENIA = request.CONTRASENIA,
                                CORREO_ELECTRONICO = request.CORREO_ELECTRONICO,
                                TELEFONO_CEL = request.TELEFONO_CEL,
                                MOTIVO_BAJA = request.MOTIVO_BAJA,
                                ID_USUARIO = request.ID_USUARIO,
                                CODIGO = request.CODIGO
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(apiRequest);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = await _ApiClient.PostAsync("API/Transaction", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "La API de login respondió {StatusCode} al autenticar.",
                        (int)response.StatusCode);

                    return ErrorGenerico("No fue posible iniciar sesión en este momento. Intenta de nuevo.");
                }

                var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent);

                if (apiResponse is null)
                {
                    _logger.LogWarning("La API de login devolvió una respuesta vacía o inválida.");
                    return ErrorGenerico("No fue posible iniciar sesión en este momento. Intenta de nuevo.");
                }

                if (apiResponse.ResponseCode != 1)
                {
                    _logger.LogWarning(
                        "La API de login rechazó la solicitud: {Mensaje}.",
                        apiResponse.ResponseMessage);

                    return ErrorGenerico("Usuario o contraseña incorrectos.");
                }

                if (apiResponse.TransactionResponses is null || apiResponse.TransactionResponses.Count == 0)
                {
                    return ErrorGenerico("Usuario o contraseña incorrectos.");
                }

                var transaction = apiResponse.TransactionResponses[0];

                // La sesión se crea únicamente cuando el backend confirmó el acceso.
                if (transaction.TransactionStatus != 1)
                {
                    _logger.LogWarning(
                        "Inicio de sesión rechazado: {Mensaje}.",
                        transaction.TransactionMessage);

                    return ErrorGenerico("Usuario o contraseña incorrectos.");
                }

                var usuarioSesion = transaction.TransactionResults?.FirstOrDefault();

                if (usuarioSesion is null)
                {
                    _logger.LogWarning("El login fue exitoso pero no devolvió datos de usuario.");
                    return ErrorGenerico("No fue posible iniciar sesión en este momento. Intenta de nuevo.");
                }

                var idUsuario = usuarioSesion.mensaje ?? string.Empty;

                if (!int.TryParse(idUsuario, out var idUsuarioNumerico) || idUsuarioNumerico <= 0)
                {
                    _logger.LogWarning("El login devolvió un ID_USUARIO no válido.");
                    return ErrorGenerico("No fue posible iniciar sesión en este momento. Intenta de nuevo.");
                }

                // Se limpia cualquier estado previo antes de crear la sesión autenticada.
                HttpContext.Session.Clear();
                HttpContext.Session.SetString("UsuarioSesion", JsonSerializer.Serialize(usuarioSesion));
                HttpContext.Session.SetString("ID_USUARIO", idUsuarioNumerico.ToString());

                return Json(new UsuarioWebRegistroResponse
                {
                    Exito = true,
                    Datos = transaction.TransactionResults ?? new List<objReponse>(),
                    Mensaje = "Consulta exitosa"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al iniciar sesión.");
                return ErrorGenerico("No fue posible iniciar sesión en este momento. Intenta de nuevo.");
            }
        }

        private static JsonResult ErrorGenerico(string mensaje) =>
            new(new UsuarioWebRegistroResponse
            {
                Exito = false,
                Mensaje = mensaje,
                Url = ""
            });

        private static JsonResult RestablecimientoFallido() =>
            new(new
            {
                exito = false,
                mensaje = "No fue posible restablecer la contraseña. Intenta de nuevo."
            });

        // Este método atiende registro (ALTA) y actualización/baja de usuarios.
        // La acción se valida contra una lista blanca y ACT/BAJA exigen sesión.
        [HttpPost]
        [EnableRateLimiting("registro")]
        public async Task<JsonResult> UsuarioWeb([FromBody] UsuarioWebRegistroRequest? request)
        {
            if (request is null)
            {
                return ErrorGenerico("Solicitud inválida.");
            }

            var accion = (request.ACCION ?? string.Empty).Trim().ToUpperInvariant();

            if (accion is not ("ALTA" or "ACT" or "BAJA" or "CONTRA"))
            {
                return ErrorGenerico("Acción no permitida.");
            }

            var idUsuarioSesion = HttpContext.Session.GetString("ID_USUARIO") ?? string.Empty;

            if (accion is "ACT" or "BAJA" &&
                (!int.TryParse(idUsuarioSesion, out var idSesion) || idSesion <= 0))
            {
                return ErrorGenerico("La sesión expiró. Inicia sesión nuevamente.");
            }

            // Para ALTA el identificador lo asigna el backend; para ACT/BAJA
            // se toma de la sesión y nunca del cuerpo de la petición.
            request.ID_USUARIO = accion == "ALTA" ? string.Empty : idUsuarioSesion;

            try
            {
                if (accion == "CONTRA" &&
                    (string.IsNullOrWhiteSpace(request.CODIGO) ||
                     string.IsNullOrWhiteSpace(request.CONTRASENIA)))
                {
                    return ErrorGenerico("El código y la nueva contraseña son requeridos.");
                }

                if (accion is "ALTA" or "ACT")
                {
                    if (string.IsNullOrWhiteSpace(request.USUARIO) ||
                        string.IsNullOrWhiteSpace(request.CONTRASENIA))
                    {
                        return ErrorGenerico("Usuario y contraseña son requeridos.");
                    }

                    if (string.IsNullOrWhiteSpace(request.CORREO_ELECTRONICO) ||
                        request.CORREO_ELECTRONICO.Length > 254 ||
                        !Regex.IsMatch(request.CORREO_ELECTRONICO, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    {
                        return ErrorGenerico("El correo electrónico no tiene un formato válido.");
                    }

                    if (string.IsNullOrWhiteSpace(request.TELEFONO_CEL) ||
                        !Regex.IsMatch(request.TELEFONO_CEL, @"^\d{10}$"))
                    {
                        return ErrorGenerico("El teléfono debe contener 10 dígitos.");
                    }

                    if (request.USUARIO.Length > 100 ||
                        !Regex.IsMatch(request.USUARIO, @"^[A-Za-zÁÉÍÓÚáéíóúÑñ ]+$"))
                    {
                        return ErrorGenerico("El nombre solo debe contener letras.");
                    }
                }

                // El registro (ALTA) exige confirmar el correo con un código OTP.
                if (accion == "ALTA")
                {
                    if (string.IsNullOrWhiteSpace(request.CODIGO) ||
                        !Regex.IsMatch(request.CODIGO.Trim(), @"^\d{6}$"))
                    {
                        return ErrorGenerico("El código de verificación es requerido.");
                    }

                    var correoNormalizado = NormalizarCorreo(request.CORREO_ELECTRONICO);
                    var claveOtp = PrefijoOtpRegistro + correoNormalizado;

                    if (!_cache.TryGetValue<OtpRegistro>(claveOtp, out var otp) ||
                        otp is null ||
                        otp.Expira < DateTime.UtcNow)
                    {
                        return ErrorGenerico("El código de verificación es inválido o expiró. Solicita uno nuevo.");
                    }

                    if (!string.Equals(otp.Codigo, request.CODIGO.Trim(), StringComparison.Ordinal))
                    {
                        var nuevosIntentos = otp.Intentos + 1;
                        _cache.Set(claveOtp, otp with { Intentos = nuevosIntentos }, TimeSpan.FromMinutes(OtpTtlMinutos));

                        return ErrorGenerico(
                            nuevosIntentos >= OtpMaxIntentos
                                ? "Demasiados intentos. Solicita un nuevo código."
                                : "El código de verificación es incorrecto.");
                    }

                    _cache.Remove(claveOtp);

                    // El código ya se validó; no debe viajar al backend.
                    request.CODIGO = string.Empty;
                }

                var apiRequest = new ApiRequest
                {
                    Application = "LoginServicios",
                    TransactionList = new List<TransactionRequest>
                    {
                        new TransactionRequest
                        {
                            TransactionName = "UsuarioWeb",
                            TransactionError = "",
                            TransactionLog = true,
                            TransactionAttributes = new UsuarioWebRegistroRequest
                            {
                                ACCION = accion,
                                USUARIO = request.USUARIO?.Trim() ?? string.Empty,
                                CONTRASENIA = request.CONTRASENIA ?? string.Empty,
                                CORREO_ELECTRONICO = request.CORREO_ELECTRONICO?.Trim() ?? string.Empty,
                                TELEFONO_CEL = request.TELEFONO_CEL?.Trim() ?? string.Empty,
                                MOTIVO_BAJA = request.MOTIVO_BAJA ?? string.Empty,
                                ID_USUARIO = request.ID_USUARIO,
                                CODIGO = request.CODIGO ?? string.Empty
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(apiRequest);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = await _ApiClient.PostAsync("API/Transaction", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "La API de usuarios respondió {StatusCode} para la acción {Accion}.",
                        (int)response.StatusCode,
                        accion);

                    return ErrorGenerico("No fue posible completar la operación. Intenta de nuevo.");
                }

                var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent);

                if (apiResponse is null)
                {
                    _logger.LogWarning("La API de usuarios devolvió una respuesta vacía o inválida.");
                    return ErrorGenerico("No fue posible completar la operación. Intenta de nuevo.");
                }

                if (apiResponse.ResponseCode != 1)
                {
                    _logger.LogWarning(
                        "La API de usuarios rechazó la acción {Accion}: {Mensaje}.",
                        accion,
                        apiResponse.ResponseMessage);

                    return ErrorGenerico(
                        apiResponse.ResponseMessage ?? "No fue posible completar la operación.");
                }

                if (apiResponse.TransactionResponses is null ||
                    apiResponse.TransactionResponses.Count == 0)
                {
                    return ErrorGenerico("No fue posible completar la operación. Intenta de nuevo.");
                }

                var transaction = apiResponse.TransactionResponses[0];

                if (transaction.TransactionStatus != 1)
                {
                    _logger.LogWarning(
                        "La transacción {Accion} falló: {Mensaje}.",
                        accion,
                        transaction.TransactionMessage);

                    return ErrorGenerico(
                        transaction.TransactionMessage ?? "No fue posible completar la operación.");
                }

                if (accion == "ALTA")
                {
                    var correoBienvenida = request.CORREO_ELECTRONICO?.Trim();
                    if (!string.IsNullOrWhiteSpace(correoBienvenida))
                    {
                        // El envío es informativo y no bloquea el registro.
                        _ = _emailService.SendEmailAsync(
                            correoBienvenida,
                            "Bienvenido al portal de COMAPA Victoria",
                            EmailTemplates.Bienvenida(
                                request.USUARIO?.Trim() ?? string.Empty,
                                correoBienvenida));
                    }
                }

                return Json(new UsuarioWebRegistroResponse
                {
                    Exito = true,
                    Datos = transaction.TransactionResults ?? new List<objReponse>(),
                    Mensaje = "Operación exitosa"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en la operación {Accion}.", accion);
                return ErrorGenerico("No fue posible completar la operación. Intenta de nuevo.");
            }
        }

        [HttpPost]
        [EnableRateLimiting("registro")]
        public async Task<JsonResult> EnviarCodigoRegistro([FromBody] EnivoCorreoRequest? userRequest)
        {
            var correo = userRequest?.UserDatos?.CORREO_ELECTRONICO?.Trim();

            if (string.IsNullOrWhiteSpace(correo) ||
                correo.Length > 254 ||
                !Regex.IsMatch(correo, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                return Json(new { exito = false, mensaje = "El correo electrónico no es válido." });
            }

            var codigo = GenerarCodigoOtp();
            var claveOtp = PrefijoOtpRegistro + NormalizarCorreo(correo);
            var otp = new OtpRegistro(codigo, DateTime.UtcNow.AddMinutes(OtpTtlMinutos), 0);

            _cache.Set(claveOtp, otp, TimeSpan.FromMinutes(OtpTtlMinutos));

            var envio = await _emailService.SendEmailAsync(
                correo,
                "Código de verificación",
                EmailTemplates.CodigoVerificacion(codigo));

            if (!envio.Exito)
            {
                _logger.LogWarning(
                    "No se pudo enviar el código de verificación a {Correo}: {Mensaje}.",
                    correo,
                    envio.Mensaje);
            }

            // En desarrollo se expone el código para poder probar el flujo sin SMTP.
            var codigoDepuracion = _entorno.IsDevelopment() ? codigo : null;

            return Json(new
            {
                exito = true,
                mensaje = "Enviamos un código de verificación a tu correo.",
                codigoDepuracion
            });
        }

        [HttpPost]
        [EnableRateLimiting("restablecimiento")]
        public async Task<JsonResult> Restablecimiento([FromBody] EnivoCorreoRequest? userRequest)
        {
            var user = userRequest?.UserDatos;

            if (user is null)
            {
                return Json(new { exito = false, mensaje = "Solicitud inválida." });
            }

            if (string.IsNullOrWhiteSpace(user.CORREO_ELECTRONICO) ||
                user.CORREO_ELECTRONICO.Length > 254 ||
                !Regex.IsMatch(user.CORREO_ELECTRONICO, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                return Json(new { exito = false, mensaje = "El correo electrónico no es válido." });
            }

            // El captcha del navegador es cosmético. La protección real es el
            // rate limit por IP y la validación del lado servidor.
            try
            {
                    const string caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                    const int longitud = 10;
                    var resultado = new StringBuilder(longitud);

                    for (var i = 0; i < longitud; i++)
                    {
                        resultado.Append(caracteres[RandomNumberGenerator.GetInt32(caracteres.Length)]);
                    }

                    user.CONTRASENIA = resultado.ToString();
                    // La acción la decide el servidor; nunca el cliente.
                    user.ACCION = "CORR";

                    // Crear la solicitud para la API externa
                    var apiRequest = new ApiRequest
                    {
                        Application = "LoginServicios",
                        TransactionList = new List<TransactionRequest>
                        {
                            new TransactionRequest
                            {
                                TransactionName = "UsuarioWeb",
                                TransactionError = "",
                                TransactionLog = true,
                                TransactionAttributes = new UsuarioWebRegistroRequest
                                {
                                    ACCION = user.ACCION,
                                    USUARIO = user.USUARIO,
                                    CONTRASENIA = user.CONTRASENIA,
                                    CORREO_ELECTRONICO = user.CORREO_ELECTRONICO,
                                    TELEFONO_CEL = user.TELEFONO_CEL,
                                    MOTIVO_BAJA = user.MOTIVO_BAJA,
                                    ID_USUARIO = user.ID_USUARIO,
                                    CODIGO = user.CODIGO
                                }
                            }
                        }
                    };

                    var json = JsonSerializer.Serialize(apiRequest);
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    using var response = await _ApiClient.PostAsync("API/Transaction", content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning(
                            "La API de restablecimiento respondió {StatusCode}.",
                            (int)response.StatusCode);

                        return RestablecimientoFallido();
                    }

                    var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent);

                    if (apiResponse is null)
                    {
                        _logger.LogWarning("La API de restablecimiento devolvió una respuesta vacía o inválida.");
                        return RestablecimientoFallido();
                    }

                    if (apiResponse.ResponseCode != 1)
                    {
                        _logger.LogWarning(
                            "La API de restablecimiento rechazó la solicitud: {Mensaje}.",
                            apiResponse.ResponseMessage);

                        return RestablecimientoFallido();
                    }

                    if (apiResponse.TransactionResponses is null ||
                        apiResponse.TransactionResponses.Count == 0)
                    {
                        return RestablecimientoFallido();
                    }

                    var transaction = apiResponse.TransactionResponses[0];

                    if (transaction.TransactionStatus != 1)
                    {
                        _logger.LogWarning(
                            "El restablecimiento falló: {Mensaje}.",
                            transaction.TransactionMessage);

                        return RestablecimientoFallido();
                    }

                    var datos = transaction.TransactionResults ?? new List<objReponse>();

                    if (datos.Count == 0)
                    {
                        _logger.LogWarning("El restablecimiento no devolvió datos de usuario.");
                        return RestablecimientoFallido();
                    }

                    user.USUARIO = (datos[0].mensaje2 ?? string.Empty).ToUpperInvariant();

                    if (!string.Equals(datos[0].mensaje, "OK", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning(
                            "El restablecimiento devolvió un estado no exitoso: {Estado}.",
                            datos[0].mensaje);

                        return RestablecimientoFallido();
                    }
                    if (string.IsNullOrWhiteSpace(user.CONTRASENIA))
                    {
                        _logger.LogWarning("No se generó la contraseña temporal.");
                        return RestablecimientoFallido();
                    }

                    var urlCambio = Url.Action(
                        "CambioContrasenia",
                        "Home",
                        null,
                        Request.Scheme);

                    var envio = await _emailService.SendEmailAsync(
                        correoDestino: user.CORREO_ELECTRONICO.Trim(),
                        asunto: "RESTABLECIMIENTO DE CONTRASEÑA",
                        mensajeCuerpo: EmailTemplates.Restablecimiento(
                            user.USUARIO,
                            user.CONTRASENIA,
                            urlCambio ?? string.Empty));

                    return Json(new { exito = envio.Exito, mensaje = envio.Mensaje });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al restablecer la contraseña.");
                return RestablecimientoFallido();
            }
        }
        
        private static string NormalizarCorreo(string correo) =>
            (correo ?? string.Empty).Trim().ToLowerInvariant();

        private static string GenerarCodigoOtp() =>
            RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        [HttpGet]
        public  JsonResult CargarDatosUsuario()
        {
            var usuario = HttpContext.Session.GetObject<objReponse>("UsuarioSesion");
            return Json(new { Exito = true, Usuario = usuario });
        }

        [HttpPost]
        public IActionResult CerrarSesion()
        {
            HttpContext.Session.Clear();
            return Json(new { ok = true });
        }
    }
}

