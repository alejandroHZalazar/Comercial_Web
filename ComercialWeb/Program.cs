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
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);


// Usar versión fija en lugar de AutoDetect (AutoDetect abre una conexión TCP en el arranque)
builder.Services.AddDbContext<ComercialDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("Default")!,
        new MySqlServerVersion(new Version(5, 5, 62))
    ));

// Railway (y cualquier reverse proxy) pasa tráfico HTTP interno aunque el cliente use HTTPS
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();   // confiar en todos los proxies (Railway)
    options.KnownProxies.Clear();
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
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

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://*:{port}");

// Configurar cultura por defecto
var defaultCulture = new CultureInfo("es-AR"); // o "es-ES"
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = new List<CultureInfo> { defaultCulture },
    SupportedUICultures = new List<CultureInfo> { defaultCulture }
};

app.UseRequestLocalization(localizationOptions);


// Leer cabeceras X-Forwarded-* antes que cualquier otro middleware
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    // UseHttpsRedirection solo en producción; Railway termina SSL en su proxy
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
