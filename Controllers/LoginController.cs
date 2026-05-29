using Microsoft.AspNetCore.Mvc;
using PrototipoComapa.Extensions;
using PrototipoComapa.Utilerias;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static PrototipoComapa.Models.API.Transactions.UsuarioWebTransactions;
using static PrototipoComapa.Models.UsuarioWeb.UsuarioWebModel;

namespace PrototipoComapa.Controllers
{
    public class LoginController : Controller
    {
        private readonly HttpClient _ApiClient;
        private readonly IConfiguration _configuration;
        //private string ENDPOINT_API = "https://financiero.comapavictoria.gob.mx/";
        //private string ENDPOINT_API = "http://192.168.1.70:62863/api";
        private readonly ILogger<LoginController> _logger;

        public LoginController(ILogger<LoginController> logger, IHttpClientFactory factory, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _ApiClient = factory.CreateClient("ApiGeneralClient");
        }

        [HttpPost]
        public async Task<JsonResult> InicioSesion([FromBody] UsuarioWebRegistroRequest request)
        {
            try
            {
                // Crear la solicitud para la API externa
                var apiRequest = new ApiRequest
                {
                    Token = _configuration["ApiSettings:Token"] ?? "tu_token_default",
                    User = _configuration["ApiSettings:User"] ?? "tu_usuario_default",
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
                                ACCION = request.ACCION,
                                USUARIO = request.USUARIO,
                                CONTRASENIA = request.CONTRASENIA,
                                CORREO_ELECTRONICO = request.CORREO_ELECTRONICO,
                                TELEFONO_CEL = request.TELEFONO_CEL,
                                MOTIVO_BAJA = request.MOTIVO_BAJA,
                                ID_USUARIO = request.ID_USUARIO
                            }
                        }
                    }
                };

                Console.WriteLine(apiRequest);

                var json = JsonSerializer.Serialize(apiRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // CORRECCIÓN: Usar la URL completa del endpoint
                var response = await _ApiClient.PostAsync("API/Transaction", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new UsuarioWebRegistroResponse
                    {
                        Exito = false,
                        Mensaje = $"Error en el servicio externo: {response.StatusCode} - {errorContent}",
                    });
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                // Log para debugging
                Console.WriteLine($"Respuesta recibida: {responseContent}");

                var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent);

                // Validar respuesta
                if (apiResponse == null)
                {
                    return Json(new UsuarioWebRegistroResponse
                    {
                        Exito = false,
                        Mensaje = "No se recibió respuesta del servicio",
                        Url = ""
                    });
                }

                if (apiResponse.ResponseCode != 1)
                {
                    return Json(new UsuarioWebRegistroResponse
                    {
                        Exito = false,
                        Mensaje = apiResponse.ResponseMessage ?? "Error en el servicio externo",
                        Url = ""
                    });
                }

                if (apiResponse.TransactionResponses == null || apiResponse.TransactionResponses.Count == 0)
                {
                    return Json(new UsuarioWebRegistroResponse
                    {
                        Exito = true,
                        Datos = new List<objReponse>(),
                        Mensaje = "No se encontraron documentos",
                        Url = ""
                    });
                }

                var transaction = apiResponse.TransactionResponses[0];

                var usuarioSesion = transaction.TransactionResults?.FirstOrDefault();
                var usuarioJson = JsonSerializer.Serialize(usuarioSesion);

                HttpContext.Session.SetString("UsuarioSesion", usuarioJson);

                if (transaction.TransactionStatus != 1)
                {
                    return Json(new UsuarioWebRegistroResponse
                    {
                        Exito = false,
                        Mensaje = transaction.TransactionMessage ?? "Error en la transacción",
                        Url = ""
                    });
                }

                var ID_USUARIO = transaction.TransactionResults.FirstOrDefault()?.mensaje ?? "";
                HttpContext.Session.SetString("ID_USUARIO", ID_USUARIO);


                return Json(new UsuarioWebRegistroResponse
                {
                    Exito = true,
                    Datos = transaction.TransactionResults ?? new List<objReponse>(),
                    Mensaje = "Consulta exitosa",
                    //User = usuarioResponse
                });
            }
            catch (Exception ex)
            {
                // Log detallado del error
                Console.WriteLine($"Error completo: {ex}");
                return Json(new UsuarioWebRegistroResponse
                {
                    Exito = false,
                    Mensaje = $"Error interno: {ex.Message}"
                });
            }
        }

        //este metodo se utiliza para el registro, actualizacion y baja de usuarios, dependiendo de la accion que se envie en el request !!!
        [HttpPost]
        public async Task<JsonResult> UsuarioWeb([FromBody] UsuarioWebRegistroRequest request) 
        {
            request.ID_USUARIO = HttpContext.Session.GetString("ID_USUARIO") ?? "";
            try
            {
                // Crear la solicitud para la API externa
                var apiRequest = new ApiRequest
                {
                    Token = _configuration["ApiSettings:Token"] ?? "tu_token_default",
                    User = _configuration["ApiSettings:User"] ?? "tu_usuario_default",
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
                                ACCION = request.ACCION,
                                USUARIO = request.USUARIO,
                                CONTRASENIA = request.CONTRASENIA,
                                CORREO_ELECTRONICO = request.CORREO_ELECTRONICO,
                                TELEFONO_CEL = request.TELEFONO_CEL,
                                MOTIVO_BAJA = request.MOTIVO_BAJA,
                                ID_USUARIO = request.ID_USUARIO
                            }
                        }
                    }
                };

                Console.WriteLine(apiRequest);

                var json = JsonSerializer.Serialize(apiRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // CORRECCIÓN: Usar la URL completa del endpoint
                var response = await _ApiClient.PostAsync("API/Transaction", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new UsuarioWebRegistroResponse
                    {
                        Exito = false,
                        Mensaje = $"Error en el servicio externo: {response.StatusCode} - {errorContent}",
                    });
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                // Log para debugging
                Console.WriteLine($"Respuesta recibida: {responseContent}");

                var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent);

                // Validar respuesta
                if (apiResponse == null)
                {
                    return Json(new UsuarioWebRegistroResponse
                    {
                        Exito = false,
                        Mensaje = "No se recibió respuesta del servicio",
                        Url = ""
                    });
                }

                if (apiResponse.ResponseCode != 1)
                {
                    return Json(new UsuarioWebRegistroResponse
                    {
                        Exito = false,
                        Mensaje = apiResponse.ResponseMessage ?? "Error en el servicio externo",
                        Url = ""
                    });
                }

                if (apiResponse.TransactionResponses == null || apiResponse.TransactionResponses.Count == 0)
                {
                    return Json(new UsuarioWebRegistroResponse
                    {
                        Exito = true,
                        Datos = new List<objReponse>(),
                        Mensaje = "No se encontraron documentos",
                        Url = ""
                    });
                }

                var transaction = apiResponse.TransactionResponses[0];

                if (transaction.TransactionStatus != 1)
                {
                    return Json(new UsuarioWebRegistroResponse
                    {
                        Exito = false,
                        Mensaje = transaction.TransactionMessage ?? "Error en la transacción",
                        Url = ""
                    });
                }

                return Json(new UsuarioWebRegistroResponse
                {
                    Exito = true,
                    Datos = transaction.TransactionResults ?? new List<objReponse>(),
                    Mensaje = "Consulta exitosa",
                });
            }
            catch (Exception ex)
            {
                // Log detallado del error
                Console.WriteLine($"Error completo: {ex}");
                return Json(new UsuarioWebRegistroResponse
                {
                    Exito = false,
                    Mensaje = $"Error interno: {ex.Message}"
                });
            }
        }

        [HttpPost]
        public async Task<JsonResult> Restablecimiento([FromBody] EnivoCorreoRequest userRequest)
        {

            var bot = userRequest.BotTest;
            var user = userRequest.UserDatos;

            if (!bot.Bot)
            {
                return Json(new { Exito = false, Mensaje = "Validacion de robot fallida" });
            }
            else
            {
                try
                {
                    const string caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                    int longitud = 6;
                    var resultado = new StringBuilder(longitud);

                    using (var rng = RandomNumberGenerator.Create())
                    {
                        var bytes = new byte[longitud];
                        rng.GetBytes(bytes);
                        foreach (var b in bytes)
                        {
                            resultado.Append(caracteres[b % caracteres.Length]);
                        }
                    }

                    user.CONTRASENIA = resultado.ToString();

                    // Crear la solicitud para la API externa
                    var apiRequest = new ApiRequest
                    {
                        Token = _configuration["ApiSettings:Token"] ?? "tu_token_default",
                        User = _configuration["ApiSettings:User"] ?? "tu_usuario_default",
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
                                    ID_USUARIO = user.ID_USUARIO
                                }
                            }
                        }
                    };

                    Console.WriteLine(apiRequest);

                    var json = JsonSerializer.Serialize(apiRequest);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // CORRECCIÓN: Usar la URL completa del endpoint
                    var response = await _ApiClient.PostAsync("API/Transaction", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        return Json(new UsuarioWebRegistroResponse
                        {
                            Exito = false,
                            Mensaje = $"Error en el servicio externo: {response.StatusCode} - {errorContent}",
                        });
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();

                    // Log para debugging
                    Console.WriteLine($"Respuesta recibida: {responseContent}");

                    var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent);

                    // Validar respuesta
                    if (apiResponse == null)
                    {
                        return Json(new UsuarioWebRegistroResponse
                        {
                            Exito = false,
                            Mensaje = "No se recibió respuesta del servicio",
                            Url = ""
                        });
                    }

                    if (apiResponse.ResponseCode != 1)
                    {
                        return Json(new UsuarioWebRegistroResponse
                        {
                            Exito = false,
                            Mensaje = apiResponse.ResponseMessage ?? "Error en el servicio externo",
                            Url = ""
                        });
                    }

                    if (apiResponse.TransactionResponses == null || apiResponse.TransactionResponses.Count == 0)
                    {
                        return Json(new UsuarioWebRegistroResponse
                        {
                            Exito = true,
                            Datos = new List<objReponse>(),
                            Mensaje = "No se encontraron documentos",
                            Url = ""
                        });
                    }

                    var transaction = apiResponse.TransactionResponses[0];

                    if (transaction.TransactionStatus != 1)
                    {
                        return Json(new UsuarioWebRegistroResponse
                        {
                            Exito = false,
                            Mensaje = transaction.TransactionMessage ?? "Error en la transacción",
                            Url = ""
                        });
                    }

                    var datos = transaction.TransactionResults ?? new List<objReponse>();
                    user.USUARIO = datos[0].mensaje2.ToUpper();

                    if (datos[0].mensaje != "OK")
                    {
                        return Json(new { exito = false, mensaje = $"{datos[0].mensaje}", Url = "" });
                    }
                    //logica para enviar correo
                    try
                    {
                        if (string.IsNullOrEmpty(user.CORREO_ELECTRONICO))
                        {
                            return Json(new { exito = false, mensaje = "El correo electrónico es requerido" });
                        }

                        if (string.IsNullOrEmpty(user.CONTRASENIA))
                        {
                            return Json(new { exito = false, mensaje = "sucedio un error al generar el codigo de recuperacion" });
                        }

                        //envio del correo
                        var emailService = new EmailService();
                        var envio = emailService.SendEmail
                        (
                            correoDestino: user.CORREO_ELECTRONICO,
                            asunto: "RESTABLECIOMIENTO DE CONTRASEÑA",
                            mensajeCuerpo: $@"
                            <h3>RECUPERACION DE CONTRASEÑA EN PORTAL COMAPA VICTORIA</h3>
                            <p>ESTIMADO (A) {user.USUARIO},</p>
                            <p>HEMOS RECIBIDO SU SOLICITUD PARA CAMBIAR LA CONTRASEÑA DE SU CUENTA EN PORTAL WEB DE COMAPA:</p>
                            <p>RECUERDE PARA INICIAR SESION DEBE INGRESAR CON LOS SIGUIENTES DATO.
                            USUARIO  <b>{user.USUARIO}</b></p>
                            CONTRASEÑA  <b>{user.CONTRASENIA}</b></p>
                            <p><b>REUCERDE CAMBIAR SU CONTRASEÑA EN SU PROXIMO INICIO DE SESION</b></p>
                            "
                        );

                        return Json(new { exito = envio[0].exito, mensaje = envio[0].mensaje });

                    }
                    catch (Exception ex)
                    {
                        return Json(new { exito = false, mensaje = $"Error interno: {ex.Message}" });
                    }
                }
                catch (Exception ex)
                {
                    // Log detallado del error
                    Console.WriteLine($"Error completo: {ex}");
                    return Json(new UsuarioWebRegistroResponse
                    {
                        Exito = false,
                        Mensaje = $"Error interno: {ex.Message}"
                    });
                }
            }
        }
        
        [HttpGet]
        public  JsonResult CargarDatosUsuario()
        {
            var usuario = HttpContext.Session.GetObject<objReponse>("UsuarioSesion");
            return Json(new { Exito = true, Usuario = usuario });
        }

        public IActionResult CerrarSesion()
        {
            HttpContext.Session.Clear();
            return Json(new { ok = true });
        }
    }
}
