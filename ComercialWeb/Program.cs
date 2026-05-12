using Application.Auth;
using Application.Security;
using Infrastructure.Auth;
using Infrastructure.Data;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Domain.Contracts;
using Infrastructure.Services;
using Application.Interfaces;
using Application.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.DataProtection;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);


// Connection string con fallback por si la variable de entorno no está configurada
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "server=72.61.47.240;database=ale;user=remoto;password=0315061;SslMode=None";

// Usar versión fija en lugar de AutoDetect (AutoDetect abre una conexión TCP en el arranque)
builder.Services.AddDbContext<ComercialDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(5, 5, 62))
    ));

// Railway (y cualquier reverse proxy) pasa tráfico HTTP interno aunque el cliente use HTTPS
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();   // confiar en todos los proxies (Railway)
    options.KnownProxies.Clear();
});

// Persistir Data Protection keys en disco — sin esto, en cada redeploy de Railway
// las cookies emitidas por el contenedor anterior se vuelven inválidas
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .SetApplicationName("ComercialWeb");

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "CW-AUTH";   // Nombre fijo — evita conflicto con cookies de deploys anteriores
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.HttpOnly = true;
    });

// Antiforgery: nombre de cookie fijo para que cookies viejas (de deploys previos
// con keys diferentes) se ignoren automáticamente
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "XSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<ICondicionIvaService, CondicionIvaService>();
builder.Services.AddScoped<IPorcentajeIvaService, PorcentajeIvaService>();
builder.Services.AddScoped<IRubroService, RubroService>();
builder.Services.AddScoped<ITipoPrecioService, TipoPrecioService>();
builder.Services.AddScoped<ITipoUsuarioService, TipoUsuarioService>();
builder.Services.AddScoped<IMenuPermisoService, MenuPermisoService>();
builder.Services.AddScoped<ILocalidadService, LocalidadService>();
builder.Services.AddScoped<IZonaClienteService, ZonaClienteService>();
builder.Services.AddScoped<IParametroService, ParametroService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IProveedorService, ProveedorService>();
builder.Services.AddScoped<IVentasEstadisticasService, VentasEstadisticasService>();
builder.Services.AddScoped<IVentasRankingService, VentasRankingService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IOrdenCompraService, OrdenCompraService>();
builder.Services.AddScoped<IIngresoProductosService, IngresoProductosService>();
builder.Services.AddScoped<IConceptoCajaService, ConceptoCajaService>();
builder.Services.AddScoped<ICajaService, CajaService>();
builder.Services.AddScoped<IMedioPagoService, MedioPagoService>();
builder.Services.AddScoped<IPlanPagoService, PlanPagoService>();
builder.Services.AddScoped<IDocumentoTipoService, DocumentoTipoService>();
builder.Services.AddScoped<IVentasExportarService, VentasExportarService>();
builder.Services.AddScoped<IDashboardVentasService, DashboardVentasService>();
builder.Services.AddScoped<IDashboardProveedorService, DashboardProveedorService>();
builder.Services.AddScoped<IDashboardClienteService,  DashboardClienteService>();
builder.Services.AddScoped<IAltaMasivaProductosService, AltaMasivaProductosService>();
builder.Services.AddScoped<IListaPreciosService, ListaPreciosService>();
builder.Services.AddScoped<IGestionStockService, GestionStockService>();
builder.Services.AddScoped<IMovimientosProductosService, MovimientosProductosService>();
builder.Services.AddScoped<ICambiosPreciosMasivoService, CambiosPreciosMasivoService>();
builder.Services.AddScoped<ICambiosPreciosService, CambiosPreciosService>();
builder.Services.AddScoped<IImpresionEtiquetasService, ImpresionEtiquetasService>();
builder.Services.AddScoped<ICuentaCorrienteService, CuentaCorrienteService>();
builder.Services.AddScoped<ICobroService, CobroService>();
builder.Services.AddScoped<INotaCreditoService, NotaCreditoService>();
builder.Services.AddScoped<IPedidoService, PedidoService>();
builder.Services.AddScoped<IFacturacionElectronicaService, FacturacionElectronicaService>();
builder.Services.AddScoped<IVentaService, VentaService>();
builder.Services.AddScoped<IReporteVentasService, ReporteVentasService>();
builder.Services.AddScoped<IDevolucionService, DevolucionService>();
builder.Services.AddScoped<IPromocionService, PromocionService>();
builder.Services.AddScoped<IFacturacionLotesService, FacturacionLotesService>();
builder.Services.AddScoped<IComprobantesFiscalesService, ComprobantesFiscalesService>();

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Leer cabeceras X-Forwarded-* PRIMERO — antes de cualquier otro middleware
// para que Request.Scheme refleje el protocolo original (https) detrás del proxy
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
else
{
    // En Railway el SSL lo termina el proxy — no redirigir acá
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Configurar cultura por defecto
var defaultCulture = new CultureInfo("es-AR");
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = new List<CultureInfo> { defaultCulture },
    SupportedUICultures = new List<CultureInfo> { defaultCulture }
};
app.UseRequestLocalization(localizationOptions);
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
