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

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

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
