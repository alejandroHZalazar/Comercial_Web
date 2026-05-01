using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Comercial_Web.Pages.Productos.Promociones;

[Authorize]
[IgnoreAntiforgeryToken]
public class EditModel : PageModel
{
    private readonly IPromocionService _promoService;

    public EditModel(IPromocionService promoService)
        => _promoService = promoService;

    // Id 0 = nuevo
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public PromocionDetalleDto Promo { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (Id > 0)
        {
            var dto = await _promoService.GetDetalleAsync(Id);
            if (dto == null) return NotFound();
            Promo = dto;
        }
        return Page();
    }

    // ── Guardar (JSON POST desde JS) ─────────────────────────────────────────
    public async Task<IActionResult> OnPostGuardarAsync([FromBody] PromocionDetalleDto dto)
    {
        if (dto.FkProducto <= 0)
            return new JsonResult(new { ok = false, error = "Debe seleccionar un producto base." });

        if (!dto.Slots.Any())
            return new JsonResult(new { ok = false, error = "Debe agregar al menos un slot." });

        foreach (var s in dto.Slots)
        {
            if (s.CantidadRequerida <= 0)
                return new JsonResult(new { ok = false, error = $"El slot {s.Numero} debe tener cantidad > 0." });
            if (!s.Productos.Any())
                return new JsonResult(new { ok = false, error = $"El slot {s.Numero} debe tener al menos un producto elegible." });
        }

        var nuevoId = await _promoService.GuardarAsync(dto);
        return new JsonResult(new { ok = true, id = nuevoId });
    }

    // ── Buscar productos para slots (autocomplete) ───────────────────────────
    public async Task<IActionResult> OnGetBuscarProductosAsync(string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return new JsonResult(new List<object>());
        var lista = await _promoService.BuscarProductosAsync(q);
        return new JsonResult(lista);
    }

    // ── Buscar producto base (single) ────────────────────────────────────────
    public async Task<IActionResult> OnGetBuscarProductoBaseAsync(string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return new JsonResult(new List<object>());
        var lista = await _promoService.BuscarProductosAsync(q);
        return new JsonResult(lista.Take(10));
    }
}
