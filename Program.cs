using AdministradorDeCuentas.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();


builder.Services.AddHttpClient("ApiGeneralClient", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(cfg["ApiSettings:BaseUrl"]);
    client.Timeout = TimeSpan.FromSeconds(20);
});

builder.Services.AddHttpClient("ApiReciboslClient", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(cfg["ApiSettings:Recibos"]);
    client.Timeout = TimeSpan.FromSeconds(20);
});

// Cliente para la API de pagos en línea (comapainternet)
builder.Services.AddHttpClient("RecibosClient", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var baseUrl = cfg["ApiSettings:Recibos"]?.Trim();
    var apiKey = cfg["ApiSettings:RecibosApiKey"]?.Trim();

    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        throw new InvalidOperationException(
            "La configuración 'ApiSettings:Recibos' es obligatoria.");
    }

    if (!baseUrl.EndsWith('/'))
    {
        baseUrl += "/";
    }

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(25);

    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-KEY", apiKey);
    }
});

// Cliente para la pasarela EVO Payments
builder.Services.AddHttpClient("EvoClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<RecibosApiService>();
builder.Services.AddScoped<EvoPaymentService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});// para variable session 
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

// Mapea las páginas de la carpeta Pages/
app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
