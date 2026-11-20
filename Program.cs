using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AdministradorDeCuentas.Services;
using PrototipoComapa.Utilerias;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var esDesarrollo = builder.Environment.IsDevelopment();

// ---------------------------------------------------------------------------
// Configuración obligatoria.
// En producción se falla al arrancar para no operar con valores por defecto.
// En desarrollo solo se advierte para no bloquear el trabajo local.
// ---------------------------------------------------------------------------
static void Requerir(IConfiguration configuration, string clave, bool esDesarrollo)
{
    if (!string.IsNullOrWhiteSpace(configuration[clave]))
    {
        return;
    }

    var mensaje =
        $"Falta la configuración obligatoria '{clave}'. " +
        "Configúrela con user-secrets en desarrollo o con variables de entorno / Key Vault en producción.";

    if (!esDesarrollo)
    {
        throw new InvalidOperationException(mensaje);
    }

    Console.Error.WriteLine($"ADVERTENCIA: {mensaje}");
}

foreach (var clave in new[]
         {
             "ApiSettings:BaseUrl",
             "ApiSettings:Recibos",
             "ApiSettings:RecibosApiKey",
             "ApiSettings:RecibosLoginUser",
             "ApiSettings:RecibosLoginPassword"
         })
{
    Requerir(builder.Configuration, clave, esDesarrollo);
}

var apiGeneralBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]?.Trim()
                        ?? "http://localhost:5058/";
var apiRecibosBaseUrl = builder.Configuration["ApiSettings:Recibos"]?.Trim()
                        ?? "http://localhost:5058/api/";

if (!apiGeneralBaseUrl.EndsWith('/'))
{
    apiGeneralBaseUrl += "/";
}

if (!apiRecibosBaseUrl.EndsWith('/'))
{
    apiRecibosBaseUrl += "/";
}

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
    {
        // Valida el token antiforgery en todos los POST/PUT/PATCH/DELETE.
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    })
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString);

builder.Services.AddRazorPages()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString);

builder.Services.AddAntiforgery(options =>
{
    // El JavaScript envía el token por cabecera; no se expone en cookies legibles.
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.Name = ".Comapa.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = esDesarrollo
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

builder.Services.AddHttpClient("ApiGeneralClient", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var apiKey = cfg["ApiSettings:RecibosApiKey"]?.Trim();

    client.BaseAddress = new Uri(apiGeneralBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(20);

    // La API de compatibilidad /API/Transaction vive ahora en ComapaInternet.Api.
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-KEY", apiKey);
    }
});

// Cliente único para ComapaInternet.Api (recibos y pagos).
builder.Services.AddHttpClient("RecibosClient", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var apiKey = cfg["ApiSettings:RecibosApiKey"]?.Trim();

    client.BaseAddress = new Uri(apiRecibosBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(25);

    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-KEY", apiKey);
    }
});

// Cliente para la pasarela EVO Payments.
builder.Services.AddHttpClient("EvoClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddMemoryCache();
builder.Services.AddScoped<RecibosApiService>();
builder.Services.AddScoped<EvoPaymentService>();
builder.Services.AddSingleton<RecibosAuthService>();
builder.Services.AddScoped<EmailService>();

builder.Services.AddSession(options =>
{
    // Margen amplio para el flujo de pago público (personas mayores o
    // captura de tarjeta pausada). La sesión es sliding.
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.Name = ".Comapa.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = esDesarrollo
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

// En Azure App Service / reverse proxy la IP real llega en X-Forwarded-For.
// Se activa por defecto fuera de desarrollo. Si el portal se expone directamente
// a Internet, desactive 'ForwardedHeaders:Enabled' para no permitir spoofing.
var usarForwardedHeaders = builder.Configuration.GetValue(
    "ForwardedHeaders:Enabled",
    !esDesarrollo);

if (usarForwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

// Rate limiting real. Sin este middleware los atributos [EnableRateLimiting]
// se ignoran silenciosamente.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString();
        }

        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            exito = false,
            mensaje = "Demasiadas solicitudes. Espera un momento e intenta de nuevo."
        }, cancellationToken);
    };

    void AgregarPolitica(string nombre, int limite, TimeSpan ventana)
    {
        options.AddPolicy(nombre, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                ClientKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limite,
                    Window = ventana,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));
    }

    AgregarPolitica("consulta-adeudo", 12, TimeSpan.FromMinutes(1));
    AgregarPolitica("iniciar-pago", 5, TimeSpan.FromMinutes(5));
    AgregarPolitica("login", 10, TimeSpan.FromMinutes(1));
    AgregarPolitica("registro", 3, TimeSpan.FromMinutes(10));
    AgregarPolitica("restablecimiento", 3, TimeSpan.FromMinutes(15));
    AgregarPolitica("recibos", 60, TimeSpan.FromMinutes(1));
});

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

var cspPortal = ConstruirCsp(builder.Configuration, !esDesarrollo);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (usarForwardedHeaders)
{
    app.UseForwardedHeaders();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    context.Response.Headers["Content-Security-Policy"] = cspPortal;

    await next();
});

app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseRateLimiter();
app.UseAuthorization();

// Mapea las páginas de la carpeta Pages/
app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static string ClientKey(HttpContext context) =>
    context.Connection.RemoteIpAddress?.ToString() ?? "desconocido";

static string ConstruirCsp(IConfiguration configuration, bool produccion)
{
    static IEnumerable<string> Leer(IConfiguration configuration, string clave) =>
        configuration.GetSection(clave).Get<string[]>() ?? Array.Empty<string>();

    var scripts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "'self'",
        "'unsafe-inline'",
        "https://cdn.jsdelivr.net",
        "https://cdnjs.cloudflare.com",
        "https://cdn.datatables.net",
        "https://*.evopayments.com",
        "https://*.gateway.mastercard.com"
    };

    var estilos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "'self'",
        "'unsafe-inline'",
        "https://cdn.jsdelivr.net",
        "https://cdnjs.cloudflare.com",
        "https://fonts.googleapis.com",
        "https://cdn.datatables.net"
    };

    var fuentes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "'self'",
        "data:",
        "https://fonts.gstatic.com",
        "https://cdnjs.cloudflare.com"
    };

    var imagenes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "'self'",
        "data:",
        "https://cdn.jsdelivr.net",
        "https://cdnjs.cloudflare.com",
        "https://cdn.datatables.net",
        "https://*.evopayments.com",
        "https://*.gateway.mastercard.com"
    };

    var conexiones = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "'self'",
        "https://cdn.datatables.net",
        "https://*.evopayments.com",
        "https://*.gateway.mastercard.com"
    };

    var marcos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "'self'",
        "https://*.evopayments.com",
        "https://*.gateway.mastercard.com"
    };

    // Origen real de la pasarela configurada (por si cambia de proveedor).
    var evo = configuration["Evo:CheckoutScriptUrl"];
    if (Uri.TryCreate(evo, UriKind.Absolute, out var evoUri) &&
        evoUri.Scheme == Uri.UriSchemeHttps)
    {
        var origenEvo = evoUri.GetLeftPart(UriPartial.Authority);
        scripts.Add(origenEvo);
        conexiones.Add(origenEvo);
        marcos.Add(origenEvo);
        imagenes.Add(origenEvo);
    }

    foreach (var origen in Leer(configuration, "Csp:AdditionalScriptOrigins")) scripts.Add(origen);
    foreach (var origen in Leer(configuration, "Csp:AdditionalStyleOrigins")) estilos.Add(origen);
    foreach (var origen in Leer(configuration, "Csp:AdditionalConnectOrigins")) conexiones.Add(origen);
    foreach (var origen in Leer(configuration, "Csp:AdditionalFrameOrigins")) marcos.Add(origen);

    var directivas = new List<string>
    {
        "default-src 'self'",
        $"script-src {string.Join(' ', scripts)}",
        $"style-src {string.Join(' ', estilos)}",
        $"font-src {string.Join(' ', fuentes)}",
        $"img-src {string.Join(' ', imagenes)}",
        $"connect-src {string.Join(' ', conexiones)}",
        // El reto 3-D Secure puede abrir marcos de emisores distintos.
        // Se permite https: solo para frames; script-src sigue restringido.
        $"frame-src {string.Join(' ', marcos)} https:",
        "frame-ancestors 'none'",
        "base-uri 'self'",
        "form-action 'self'",
        "object-src 'none'"
    };

    if (produccion)
    {
        directivas.Add("upgrade-insecure-requests");
    }

    return string.Join("; ", directivas) + ";";
}
