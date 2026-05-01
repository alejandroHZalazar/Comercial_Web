using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Comercial_Web.Pages.Productos.CambiosPreciosMasivo;

[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly ICambiosPreciosMasivoService _service;

    public IndexModel(ICambiosPreciosMasivoService service)
        => _service = service;

    public void OnGet() { }

    // ---------------------------------------------------------------
    // POST: procesar una fila del CSV (llamado row-by-row desde JS)
    // ---------------------------------------------------------------
    public async Task<JsonResult> OnPostProcesarFilaAsync(
        [FromBody] FilaCsvPrecioDto fila)
    {
        var result = await _service.CambiarPrecioAsync(fila);
        return new JsonResult(result,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    // ---------------------------------------------------------------
    // POST: insertar un producto nuevo desde la grilla de no encontrados
    // ---------------------------------------------------------------
    public async Task<JsonResult> OnPostInsertarNuevoAsync(
        [FromBody] InsertarProductoNuevoRequest req)
    {
        var result = await _service.InsertarProductoNuevoAsync(req);
        return new JsonResult(result,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}
