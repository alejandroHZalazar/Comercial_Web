using System.Security.Claims;
using Application.Interfaces;
using Domain.Contracts;
using Domain.DTO;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Comercial_Web.Pages.Productos.PromocionesDescuentos;

/// <summary>Alta / edición de una promoción por cantidad (NxM) o por porcentaje, con selección de productos.</summary>
[Authorize]
[IgnoreAntiforgeryToken]
public class EditModel : PageModel
{
    private const int MaxResultadosPantalla = 300;     // resultados que se listan al buscar
    private const int MaxAgregarTodos       = 3000;    // tope de "agregar todos los resultados"

    private readonly IPromocionDescuentosService _service;
    private readonly IProveedorService           _proveedorService;
    private readonly IRubroService               _rubroService;

    public EditModel(IPromocionDescuentosService service, IProveedorService proveedorService, IRubroService rubroService)
    {
        _service          = service;
        _proveedorService = proveedorService;
        _rubroService     = rubroService;
    }

    [BindProperty(SupportsGet = true)] public int Id { get; set; }

    public PromoReglaDetalleDto Regla        { get; private set; } = new();
    public bool                 TablasListas { get; private set; }
    public List<SelectListItem> Proveedores  { get; private set; } = new();
    public List<SelectListItem> Rubros       { get; private set; } = new();
    public int                  MaxPantalla  => MaxResultadosPantalla;

    public async Task<IActionResult> OnGetAsync()
    {
        TablasListas = await _service.TablasListasAsync();
        if (!TablasListas) return Page();

        if (Id > 0)
        {
            var r = await _service.ObtenerAsync(Id);
            if (r == null) return NotFound();
            Regla = r;
        }
        else
        {
            var ahora = HoraArgentina.Ahora();
            Regla = new PromoReglaDetalleDto
            {
                Tipo = "PORC",
                Desde = new DateTime(ahora.Year, ahora.Month, ahora.Day, ahora.Hour, ahora.Minute, 0),
                Hasta = new DateTime(ahora.Year, ahora.Month, ahora.Day, 23, 59, 0).AddDays(30)
            };
        }

        var provs = await _proveedorService.GetAllAsync();
        Proveedores = provs.Where(p => p.Baja != true).OrderBy(p => p.NombreComercial)
            .Select(p => new SelectListItem(p.NombreComercial ?? p.Id.ToString(), p.Id.ToString())).ToList();
        var rubs = await _rubroService.GetAllConRutaAsync();
        Rubros = rubs.Select(r => new SelectListItem(r.Ruta, r.Id.ToString())).ToList();
        return Page();
    }

    // ── Búsqueda de productos: proveedor, rubro, código de barras / de proveedor / descripción ──
    public async Task<IActionResult> OnGetBuscarProductosAsync(
        [FromQuery] int[] proveedores, [FromQuery] int[] rubros, [FromQuery] string? texto, [FromQuery] bool todos = false)
    {
        if (proveedores.Length == 0 && rubros.Length == 0 && string.IsNullOrWhiteSpace(texto))
            return new JsonResult(new { ok = false, error = "Ingrese al menos un criterio de búsqueda." });

        var r = await _service.BuscarProductosAsync(proveedores, rubros, texto, todos ? MaxAgregarTodos : MaxResultadosPantalla);
        return new JsonResult(new { ok = true, total = r.Total, items = r.Items, limite = todos ? MaxAgregarTodos : MaxResultadosPantalla },
            new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
    }

    // ── Guardar (JSON desde el navegador) ──────────────────────────────────
    public async Task<IActionResult> OnPostGuardarAsync([FromBody] PromoReglaDetalleDto dto)
    {
        try
        {
            var usuario = User.FindFirstValue(ClaimTypes.Name);
            var id = await _service.GuardarAsync(dto, usuario);
            return new JsonResult(new { ok = true, id });
        }
        catch (InvalidOperationException ex)
        {
            return new JsonResult(new { ok = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { ok = false, error = "Error al guardar: " + ex.Message });
        }
    }
}
