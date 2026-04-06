using Application.Auth;
using Application.Security;
using Infrastructure.Auth;
using Infrastructure.Data;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Domain.Contracts;
using Infrastructure.Services;
using Application.Interfaces;
using Application.Services;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<ComercialDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("Default"),
        ServerVersion.AutoDetect(
            builder.Configuration.GetConnectionString("Default")
        )
    ));

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

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configurar cultura por defecto
var defaultCulture = new CultureInfo("es-AR"); // o "es-ES"
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = new List<CultureInfo> { defaultCulture },
    SupportedUICultures = new List<CultureInfo> { defaultCulture }
};

app.UseRequestLocalization(localizationOptions);


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{

    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
