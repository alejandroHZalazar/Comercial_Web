using Domain.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Comercial_Web.Pages.Configuracion;

public class LogoModel : PageModel
{
    private readonly IParametroService _service;

    public LogoModel(IParametroService service)
    {
        _service = service;
    }

    [BindProperty]
    public IFormFile? ArchivoLogo { get; set; }

    public string? Mensaje { get; set; }

    public async Task<IActionResult> OnPostInsertarAsync()
    {
        if (ArchivoLogo is null || ArchivoLogo.Length == 0)
        {
            ModelState.AddModelError("", "Debe seleccionar un archivo.");
            return Page();
        }

        using var ms = new MemoryStream();
        await ArchivoLogo.CopyToAsync(ms);
        var contenido = ms.ToArray();
        var nombreArchivo = Path.GetFileName(ArchivoLogo.FileName);

        await _service.UpdateLogoAsync(nombreArchivo, contenido);

        Mensaje = "Proceso completo";
        return Page();
    }

    public async Task OnGetAsync()
    {
        var logo = await _service.GetLogoAsync();
        if (logo is not null)
        {
            Mensaje = $"Logo actual: {logo.Valor}";
        }
    }

    public async Task<IActionResult> OnGetLogoAsync()
    {
        var logo = await _service.GetLogoAsync();
        if (logo is null || logo.Imagen is null)
        {
            return NotFound();
        }

        // Podés ajustar el content-type según el tipo de imagen que guardes (ej: "image/png")
        return File(logo.Imagen, "image/bmp");
    }

}
