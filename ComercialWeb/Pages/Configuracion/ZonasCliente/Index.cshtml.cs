using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Comercial_Web.Pages.Configuracion.ZonasClientes;

public class IndexModel : PageModel
{
    private readonly IZonaClienteService _service;

    public IndexModel(IZonaClienteService service)
    {
        _service = service;
    }

    [BindProperty] public int? Id { get; set; }
    [BindProperty] public string Nombre { get; set; } = string.Empty;
    public List<ClientesZona> Items { get; set; } = new();
    public bool MostrarFormulario { get; set; }

    public async Task OnGetAsync()
    {
        await CargarGrillaAsync();
    }

    public async Task<IActionResult> OnPostNuevoAsync()
    {
        MostrarFormulario = true;
        await CargarGrillaAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetEditarAsync(int id)
    {
        var zona = await _service.GetByIdAsync(id);
        if (zona is null) return NotFound();

        Id = zona.Id;
        Nombre = zona.Nombre ?? string.Empty;
        MostrarFormulario = true;

        await CargarGrillaAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostGuardarNuevoAsync()
    {
        if (!FormularioValido()) return await ReintentarAsync();

        await _service.CreateAsync(Nombre.Trim());
        MostrarFormulario = true;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGuardarEdicionAsync()
    {
        if (!Id.HasValue || !FormularioValido()) return await ReintentarAsync();

        await _service.UpdateAsync(Id.Value, Nombre.Trim());
        MostrarFormulario = true;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        await _service.DeleteAsync(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelarAsync()
    {
        MostrarFormulario = true;
        return RedirectToPage();
    }

    private async Task<IActionResult> ReintentarAsync()
    {
        MostrarFormulario = true;
        await CargarGrillaAsync();
        ModelState.AddModelError("", "Debe completar el nombre.");
        return Page();
    }

    private bool FormularioValido()
    {
        return !string.IsNullOrWhiteSpace(Nombre);
    }

    private async Task CargarGrillaAsync()
    {
        Items = await _service.GetAllAsync();
    }
}