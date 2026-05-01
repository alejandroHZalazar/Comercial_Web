using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Comercial_Web.Pages.Productos.Promociones;

[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly IPromocionService _promoService;

    public IndexModel(IPromocionService promoService)
        => _promoService = promoService;

    public List<PromocionListDto> Promociones { get; private set; } = new();

    public async Task OnGetAsync()
        => Promociones = await _promoService.GetListaAsync();

    // ── Toggle activa/inactiva ────────────────────────────────────────────────
    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        await _promoService.ToggleActivaAsync(id);
        return new JsonResult(new { ok = true });
    }

    // ── Eliminar ─────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        await _promoService.EliminarAsync(id);
        return new JsonResult(new { ok = true });
    }
}
