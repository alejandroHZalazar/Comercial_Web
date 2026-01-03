using Domain.Entities;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Comercial_Web.Pages.Configuracion.Localidades;
[Authorize]
public class IndexModel : PageModel
{
    private readonly ILocalidadService _service;

    public IndexModel(ILocalidadService service)
    {
        _service = service;
    }

    [BindProperty] public int? Id { get; set; }
    [BindProperty] public string Nombre { get; set; } = string.Empty;
    [BindProperty] public int? FkProvincia { get; set; }
    [BindProperty] public int? FiltroProvinciaId { get; set; }

    public List<Localidade> Items { get; set; } = new();
    public List<SelectListItem> Provincias { get; set; } = new();
    public bool MostrarFormulario { get; set; }

    public async Task OnGetAsync()
    {
        await CargarCombosAsync();
        await CargarGrillaAsync();
    }

    public async Task<IActionResult> OnPostFiltrarAsync()
    {
        await CargarCombosAsync();
        await CargarGrillaAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostNuevoAsync()
    {
        MostrarFormulario = true;
        await CargarCombosAsync();
        await CargarGrillaAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetEditarAsync(int id)
    {
        var loc = await _service.GetByIdAsync(id);
        if (loc is null) return NotFound();

        Id = loc.Id;
        Nombre = loc.Nombre ?? string.Empty;
        FkProvincia = loc.FkProvincia;

        MostrarFormulario = true;
        await CargarCombosAsync();
        await CargarGrillaAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostGuardarNuevoAsync()
    {
        if (!FormularioValido()) return await ReintentarAsync();

        await _service.CreateAsync(new Localidade
        {
            Nombre = Nombre.Trim(),
            FkProvincia = FkProvincia
        });

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGuardarEdicionAsync()
    {
        if (!Id.HasValue || !FormularioValido()) return await ReintentarAsync();

        await _service.UpdateAsync(new Localidade
        {
            Id = Id.Value,
            Nombre = Nombre.Trim(),
            FkProvincia = FkProvincia
        });

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        await _service.DeleteAsync(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelarAsync()
    {
        return RedirectToPage();
    }

    private async Task<IActionResult> ReintentarAsync()
    {
        MostrarFormulario = true;
        await CargarCombosAsync();
        await CargarGrillaAsync();
        ModelState.AddModelError("", "Debe completar todos los campos.");
        return Page();
    }

    private bool FormularioValido()
    {
        return !string.IsNullOrWhiteSpace(Nombre) && FkProvincia.HasValue;
    }

    private async Task CargarCombosAsync()
    {
        var provincias = await _service.GetProvinciasAsync();
        Provincias = provincias.Select(p => new SelectListItem
        {
            Value = p.Id.ToString(),
            Text = p.Nombre
        }).ToList();
    }

    private async Task CargarGrillaAsync()
    {
        Items = await _service.GetByProvinciaAsync(FiltroProvinciaId);
    }
}