using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Comercial_Web.Pages.Productos.PromocionesDescuentos;

/// <summary>Listado de promociones por cantidad (NxM) y por porcentaje.</summary>
[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly IPromocionDescuentosService _service;
    public IndexModel(IPromocionDescuentosService service) => _service = service;

    public List<PromoReglaListDto> Reglas        { get; private set; } = new();
    public bool                    TablasListas  { get; private set; }
    public bool                    MotorActivo   { get; private set; }

    public async Task OnGetAsync()
    {
        TablasListas = await _service.TablasListasAsync();
        MotorActivo  = await _service.HabilitadoAsync();
        if (TablasListas) Reglas = await _service.ListarAsync();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        try   { return new JsonResult(new { ok = true, activa = await _service.ToggleActivaAsync(id) }); }
        catch (Exception ex) { return new JsonResult(new { ok = false, error = ex.Message }); }
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        try   { await _service.EliminarAsync(id); return new JsonResult(new { ok = true }); }
        catch (Exception ex) { return new JsonResult(new { ok = false, error = ex.Message }); }
    }
}
