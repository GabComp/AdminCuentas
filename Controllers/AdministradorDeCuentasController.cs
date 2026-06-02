using AdministradorDeCuentas.Filters;
using AdministradorDeCuentas.Models.APIRecibos;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
//using Newtonsoft.Json;
using System.Text;
using System.Text.Json;
using static AdministradorDeCuentas.Models.API.Transactions.CunetaUsuariosTransaction;
using static APIPipas.Models.CuentaUsuarios.CuentaUsuarios;
using static PrototipoComapa.Models.API.Transactions.UsuarioWebTransactions;
//using static PrototipoComapa.Models.UsuarioWeb.UsuarioWebModel;


namespace AdministradorDeCuentas.Controllers
{
    [ValidarSesion]
    public class AdministradorDeCuentasController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        //private readonly HttpClient _ApiClient;

        public AdministradorDeCuentasController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
             _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            //_ApiClient = factory.CreateClient("ApiGeneralClient");
        }
        public IActionResult AdministradorDeCuentas()
        {
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> LoginCuentas([FromBody] RecibosModel requestt)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiReciboslClient");

                var json = JsonConvert.SerializeObject(requestt);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));

                var response = await client.PostAsync("auth/login", content, cts.Token);

                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new
                    {
                        ok = false,
                        status = (int)response.StatusCode,
                        message = "Error al autenticar con API Recibos",
                        detail = body
                    });
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    return Json(new
                    {
                        ok = false,
                        message = "La API respondió vacío"
                    });
                }

                var tokenResponse = JsonConvert.DeserializeObject<RecibosResponse>(body);

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.token))
                {
                    return Json(new
                    {
                        ok = false,
                        message = "No se pudo obtener el token de la respuesta"
                    });
                }

                HttpContext.Session.SetString("TokenRecibos", tokenResponse.token);

                return Json(new
                {
                    ok = true,
                    //body = tokenResponse,
                    //response = response
                });
            }
            catch (TaskCanceledException)
            {
                return Json(new
                {
                    ok = false,
                    message = "Timeout al conectar con API Recibos"
                });
            }
            catch (HttpRequestException ex)
            {
                return Json(new
                {
                    ok = false,
                    message = "Error de conexión con API Recibos",
                    detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    ok = false,
                    message = "Error desconocido",
                    detail = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<JsonResult> ValidarCuentasUsuarios([FromBody] DatosValidacion Jcontent)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiReciboslClient");
                var request = new HttpRequestMessage(HttpMethod.Post, $"{client.BaseAddress}comercial/ValidaCuenta");
                var token = HttpContext.Session.GetString("TokenRecibos") ?? "";
                request.Headers.Add("X-API-KEY", "bXre5Bh8UJrfryj5ZnvWQ5IE90jiw1OcaX6o");
                request.Headers.Add("Authorization", $"Bearer {token}");

                var json = JsonConvert.SerializeObject(Jcontent);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                request.Content = content;

                var response = await client.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new
                    {
                        ok = false,
                        status = (int)response.StatusCode,
                        message = "Error al autenticar con API Recibos",
                        detail = body
                    });
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    return Json(new
                    {
                        ok = false,
                        message = "La API respondió vacío"
                    });
                }

                var lista = JsonConvert.DeserializeObject<List<DatosValidacionResponse>>(body);

                return Json(new
                {
                    ok = true,
                    body = lista,
                    response = response
                });
            }
            catch (TaskCanceledException)
            {
                return Json(new
                {
                    ok = false,
                    message = "Timeout al conectar con API Recibos"
                });
            }
            catch (HttpRequestException ex)
            {
                return Json(new
                {
                    ok = false,
                    message = "Error de conexión con API Recibos",
                    detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    ok = false,
                    message = "Error desconocido",
                    detail = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<JsonResult> ConsultarCuentasUsuario([FromBody] USUARIO_WEB_CUENTA request)
        {
            try
            {
                var idUseuario = HttpContext.Session.GetString("ID_USUARIO") ?? "";
                request.ID_USUARIO_WEB = Convert.ToInt16(idUseuario);
                var apiRequest = new CuentaUsuarioApiRequest
                {
                    Token = _configuration["ApiSettings:Token"] ?? "tu_token_default",
                    User = _configuration["ApiSettings:User"] ?? "tu_usuario_default",
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
                                ID_USUARIO_WEB = request.ID_USUARIO_WEB,
                            }
                        }
                    }
                };
                Console.WriteLine(apiRequest);
                var client = _httpClientFactory.CreateClient("ApiGeneralClient");
                var json = System.Text.Json.JsonSerializer.Serialize(apiRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // CORRECCIÓN: Usar la URL completa del endpoint
                var response = await client.PostAsync("API/Transaction", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new CuentasUsuarioResponse
                    {
                        Exito = false,
                        Mensaje = $"Error en el servicio externo: {response.StatusCode} - {errorContent}",
                    });
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                // Log para debugging
                Console.WriteLine($"Respuesta recibida: {responseContent}");

                var apiResponse = System.Text.Json.JsonSerializer.Deserialize<ApiCunetaUsuariosResponse>(responseContent);

                // Validar respuesta
                if (apiResponse == null)
                {
                    return Json(new CuentasUsuarioResponse
                    {
                        Exito = false,
                        Mensaje = "No se recibió respuesta del servicio",
                        //Url = ""
                    });
                }

                if (apiResponse.ResponseCode != 1)
                {
                    return Json(new CuentasUsuarioResponse
                    {
                        Exito = false,
                        Mensaje = apiResponse.ResponseMessage ?? "Error en el servicio externo",
                        //Url = ""
                    });
                }

                if (apiResponse.TransactionResponses == null || apiResponse.TransactionResponses.Count == 0)
                {
                    return Json(new CuentasUsuarioResponse
                    {
                        Exito = true,
                        Datos = new List<USUARIO_WEB_CUENTA_Result>(),
                        Mensaje = "No se encontraron documentos",
                    });
                }

                var transaction = apiResponse.TransactionResponses[0];

                var usuarioSesion = transaction.TransactionResults?.FirstOrDefault();
                var cuentas = transaction.TransactionResults?.Select(x => new { x.CUENTA_COMAPA, x.ALIAS_CUENTA } ).ToList();
                var cuentasJson = System.Text.Json.JsonSerializer.Serialize(cuentas);

                HttpContext.Session.SetString("CuentasUsuario", cuentasJson);

                if (transaction.TransactionStatus != 1)
                {
                    return Json(new CuentasUsuarioResponse
                    {
                        Exito = false,
                        Mensaje = transaction.TransactionMessage ?? "Error en la transacción",
                    });
                }

                return Json(new CuentasUsuarioResponse
                {
                    Exito = true,
                    Datos = transaction.TransactionResults ?? new List<USUARIO_WEB_CUENTA_Result>(),
                    Mensaje = "Consulta exitosa",
                    //User = usuarioResponse
                });
            }
            catch (Exception ex)
            {
                // Log detallado del error
                Console.WriteLine($"Error completo: {ex}");
                return Json(new CuentasUsuarioResponse
                {
                    Exito = false,
                    Mensaje = $"Error interno: {ex.Message}"
                });
            }
        }

        [HttpPost]
        public async Task<JsonResult> ActionsCuentaUsuario([FromBody] ACTIONS_USUARIO_WEB_CUENTA request)
        {
            try
            {
                var idUseuario = HttpContext.Session.GetString("ID_USUARIO") ?? "";
                request.ID_USUARIO_WEB = Convert.ToInt16(idUseuario);
                // Crear la solicitud para la API externa
                var apiRequest = new ActionsApiRequest
                {
                    Token = _configuration["ApiSettings:Token"] ?? "tu_token_default",
                    User = _configuration["ApiSettings:User"] ?? "tu_usuario_default",
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
                                ACCION = request.ACCION,
                                ID_USUARIO_WEB = request.ID_USUARIO_WEB,
                                CUENTA_COMAPA = request.CUENTA_COMAPA,
                                MOTIVO_BAJA = request.MOTIVO_BAJA,
                                ID_IMAGEN_AUTORIZA_PAPERLESS = request.ID_IMAGEN_AUTORIZA_PAPERLESS,
                                ALIAS_CUENTA = request.ALIAS_CUENTA,
                                Nombre_Propietario = request.Nombre_Propietario,
                                PAGO_TOTAL = request.PAGO_TOTAL
                            }
                        }
                    }
                };

                Console.WriteLine(apiRequest);

                var json = System.Text.Json.JsonSerializer.Serialize(apiRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var client = _httpClientFactory.CreateClient("ApiGeneralClient");
                // CORRECCIÓN: Usar la URL completa del endpoint
                var response = await client.PostAsync("API/Transaction", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new ActionsCuentasResponse
                    {
                        Exito = false,
                        Mensaje = $"Error en el servicio externo: {response.StatusCode} - {errorContent}",
                    });
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                // Log para debugging
                Console.WriteLine($"Respuesta recibida: {responseContent}");

                var apiResponse = System.Text.Json.JsonSerializer.Deserialize<ApiActionsCunetaResponse>(responseContent);

                // Validar respuesta
                if (apiResponse == null)
                {
                    return Json(new ActionsCuentasResponse
                    {
                        Exito = false,
                        Mensaje = "No se recibió respuesta del servicio",
                    });
                }

                if (apiResponse.ResponseCode != 1)
                {
                    return Json(new ActionsCuentasResponse
                    {
                        Exito = false,
                        Mensaje = apiResponse.ResponseMessage ?? "Error en el servicio externo",
                    });
                }

                if (apiResponse.TransactionResponses == null || apiResponse.TransactionResponses.Count == 0)
                {
                    return Json(new ActionsCuentasResponse
                    {
                        Exito = true,
                        Datos = new List<ACTIONS_USUARIO_WEB_CUENTAResult>(),
                        Mensaje = "No se encontraron documentos",
                    });
                }

                var transaction = apiResponse.TransactionResponses[0];

                if (transaction.TransactionStatus != 1)
                {
                    return Json(new ActionsCuentasResponse
                    {
                        Exito = false,
                        Mensaje = transaction.TransactionMessage ?? "Error en la transacción",
                    });
                }

                return Json(new ActionsCuentasResponse
                {
                    Exito = true,
                    Datos = transaction.TransactionResults ?? new List<ACTIONS_USUARIO_WEB_CUENTAResult>(),
                    Mensaje = "Consulta exitosa",
                });
            }
            catch (Exception ex)
            {
                // Log detallado del error
                Console.WriteLine($"Error completo: {ex}");
                return Json(new ActionsCuentasResponse
                {
                    Exito = false,
                    Mensaje = $"Error interno: {ex.Message}"
                });
            }
        }
    }
}
