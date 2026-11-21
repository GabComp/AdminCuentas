using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AdministradorDeCuentas.Filters
{
    /// <summary>
    /// Valida que exista una sesión de ciudadano autenticada.
    /// La sesión solo se crea en LoginController cuando el backend confirma
    /// el acceso; aquí se exige además que el identificador de usuario sea válido.
    ///
    /// Se ejecuta como filtro de autorización con prioridad (Order = -1000) para
    /// que una sesión expirada responda 401 antes de que corra la validación
    /// antiforgery (que de otro modo devolvería 400 con un token viejo).
    /// </summary>
    public sealed class ValidarSesionAttribute : Attribute, IAsyncAuthorizationFilter, IOrderedFilter
    {
        public int Order => -1000;

        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var usuarioJson = context.HttpContext.Session.GetString("UsuarioSesion");
            var idUsuario = context.HttpContext.Session.GetString("ID_USUARIO");

            var sesionValida =
                !string.IsNullOrWhiteSpace(usuarioJson) &&
                !string.Equals(usuarioJson, "null", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(idUsuario) &&
                int.TryParse(idUsuario, out var id) &&
                id > 0;

            if (!sesionValida)
            {
                context.HttpContext.Session.Clear();

                if (EsSolicitudAjax(context.HttpContext.Request))
                {
                    context.Result = new JsonResult(new
                    {
                        exito = false,
                        mensaje = "La sesión expiró. Inicia sesión nuevamente."
                    })
                    {
                        StatusCode = StatusCodes.Status401Unauthorized
                    };
                }
                else
                {
                    context.Result = new RedirectToRouteResult(
                        new RouteValueDictionary
                        {
                            { "controller", "Home" },
                            { "action", "Index" }
                        });
                }
            }

            return Task.CompletedTask;
        }

        private static bool EsSolicitudAjax(HttpRequest request)
        {
            return string.Equals(
                       request.Headers["X-Requested-With"],
                       "XMLHttpRequest",
                       StringComparison.OrdinalIgnoreCase) ||
                   request.Headers.Accept.Any(valor =>
                       valor?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
        }
    }
}
