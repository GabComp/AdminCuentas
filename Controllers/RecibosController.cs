using AdministradorDeCuentas.Filters;
using AdministradorDeCuentas.Models.APIRecibos;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PrototipoComapa.Models.API.Transactions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static PrototipoComapa.Models.API.Transactions.UsuarioRecibos;
using static PrototipoComapa.Models.API.Transactions.UsuarioWebTransactions;
using static PrototipoComapa.Models.UsuarioWeb.UsuarioWebModel;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace PrototipoComapa.Controllers.Servicios.TramitesDisponibles
{
    [ValidarSesion]
    public class RecibosController : Controller
    {
        //private readonly HttpClient _ApiRecibos;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public RecibosController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // GET: RecibosController
        public ActionResult Recibos()
        {
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> LoginRecibos([FromBody] RecibosModel requestt)
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

        public async Task<JsonResult> ValidarLoginRecibos([FromBody] DatosValidacion Jcontent)
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

        public async Task<JsonResult> ConsultaRecibos([FromBody] DatosValidacion Jcontent)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiReciboslClient");
                var request = new HttpRequestMessage(HttpMethod.Post, $"{client.BaseAddress}comercial/HistoriaRecibos");
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

                var lista = JsonConvert.DeserializeObject<List<ConsultaRecibosResponse>>(body);

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

        public async Task<JsonResult> ConsultaReciboPDF([FromBody] DatosValidacion Jcontent)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiReciboslClient");
                var request = new HttpRequestMessage(HttpMethod.Post, $"{client.BaseAddress}comercial/ObtnRecibo");
                var token = HttpContext.Session.GetString("TokenRecibos") ?? "";
                request.Headers.Add("X-API-KEY", "bXre5Bh8UJrfryj5ZnvWQ5IE90jiw1OcaX6o");
                request.Headers.Add("Authorization", $"Bearer {token}");

                var json = JsonConvert.SerializeObject(Jcontent);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                request.Content = content;

                var response = await client.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                var pdfBytes = await response.Content.ReadAsByteArrayAsync();

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

                return Json(new
                {
                    ok = true,
                    body = pdfBytes,
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

        public IActionResult ObtenerCuentasRecibosUsuario()
        {
            var cuentasJson = HttpContext.Session.GetString("CuentasUsuario");

            return Content(cuentasJson, "application/json");
        }
    }
}
