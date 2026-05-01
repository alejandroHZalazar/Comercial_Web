using Application.Interfaces;
using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;

namespace Comercial_Web.Pages.Productos.CambiosPrecios;

[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly ICambiosPreciosService  _service;
    private readonly IProveedorService      _proveedorService;
    private readonly IRubroService          _rubroService;
    private readonly IParametroService      _parametroService;

    public List<SelectListItem> Proveedores { get; private set; } = new();
    public List<SelectListItem> Rubros      { get; private set; } = new();
    public int Decimales { get; private set; } = 2;

    public IndexModel(
        ICambiosPreciosService  service,
        IProveedorService       proveedorService,
        IRubroService           rubroService,
        IParametroService       parametroService)
    {
        _service          = service;
        _proveedorService = proveedorService;
        _rubroService     = rubroService;
        _parametroService = parametroService;
    }

    // ---------------------------------------------------------------
    // GET — carga inicial
    // ---------------------------------------------------------------
    public async Task OnGetAsync()
    {
        var provs = await _proveedorService.GetAllAsync();
        Proveedores = provs
            .Where(p => p.Baja != true)
            .OrderBy(p => p.NombreComercial)
            .Select(p => new SelectListItem(
                p.NombreComercial ?? p.Id.ToString(),
                p.Id.ToString()))
            .ToList();

        var rubs = await _rubroService.GetAllAsync();
        Rubros = rubs
            .OrderBy(r => r.Descripcion)
            .Select(r => new SelectListItem(
                r.Descripcion ?? r.Id.ToString(),
                r.Id.ToString()))
            .ToList();

        var decStr = await _parametroService.ObtenerValorAsync("productos", "decimales");
        if (int.TryParse(decStr, out var dec) && dec >= 0 && dec <= 6)
            Decimales = dec;
    }

    // ---------------------------------------------------------------
    // GET Buscar → JSON
    // ---------------------------------------------------------------
    public async Task<JsonResult> OnGetBuscarAsync(
        [FromQuery] int[]   proveedores,
        [FromQuery] int[]   rubros,
        [FromQuery] string? texto)
    {
        var items = await _service.BuscarAsync(
            proveedores.Length > 0 ? proveedores : null,
            rubros.Length      > 0 ? rubros      : null,
            texto);

        return new JsonResult(items,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    // ---------------------------------------------------------------
    // POST ActualizarFila → recibe los 3 precios ya calculados en cliente
    // Soporta ambos modos: "desde proveedor" e "independiente"
    // ---------------------------------------------------------------
    public async Task<JsonResult> OnPostActualizarFilaAsync(
        [FromBody] ActualizarPrecioDirectoRequest req)
    {
        var result = await _service.ActualizarPrecioDirectoAsync(req);
        return new JsonResult(result,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}
