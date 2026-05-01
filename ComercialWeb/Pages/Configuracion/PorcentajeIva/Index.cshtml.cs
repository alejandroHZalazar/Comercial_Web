using Domain.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Domain.Entities;

namespace Comercial_Web.Pages.Configuracion.PorcentajeIva;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IPorcentajeIvaService _service;
    public IndexModel(IPorcentajeIvaService service) => _service = service;

    public List<IvaPorcentaje> Items { get; private set; } = new();

    [BindProperty] public int? Id { get; set; }
    [BindProperty] public decimal? Valor { get; set; }


    public bool MostrarFormulario { get; set; }

    public async Task<IActionResult> OnGetAsync(int? editarId = null)
    {
        Items = await _service.GetAllAsync();

        if (editarId.HasValue)
        {
            var item = await _service.GetByIdAsync(editarId.Value);
            if (item is null) return NotFound();

            Id = item.Id;
            Valor = item.Valor;
            MostrarFormulario = true; 
        }

        return Page();
    }

    public async Task<IActionResult> OnPostNuevoAsync()
    {
        
        MostrarFormulario = true;
        Items = await _service.GetAllAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostGuardarNuevoAsync()
    {
        if (Valor <= 0)
        {
            ModelState.AddModelError(nameof(Valor), "Indique un porcentaje de IVA");
            Items = await _service.GetAllAsync();
            MostrarFormulario = true; 
            return Page();
        }

        await _service.CreateAsync(Valor ?? 0);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGuardarEdicionAsync()
    {
        if (!Id.HasValue || Valor <= 0)
        {
            ModelState.AddModelError(nameof(Valor), "Indique un porcentaje válido");
            Items = await _service.GetAllAsync();
            MostrarFormulario = true; 
            return Page();
        }

        await _service.UpdateAsync(Id.Value, Valor ?? 0);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        await _service.DeleteAsync(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetEditarAsync(int id)
    {
        var loc = await _service.GetByIdAsync(id);
        if (loc is null) return NotFound();

        Id = loc.Id;
        Valor = loc.Valor;
        MostrarFormulario = true;

        Items = await _service.GetAllAsync();
        return Page();
    }


    public async Task<IActionResult> OnPostCancelarAsync()
    {
       
        return RedirectToPage();
    }
}