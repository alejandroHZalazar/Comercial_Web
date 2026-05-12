using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Comercial_Web.Pages.Facturacion.ComprobantesFiscales;

[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly IComprobantesFiscalesService _service;

    public IndexModel(IComprobantesFiscalesService service) => _service = service;

    // ── Filtros bindeados ────────────────────────────────────────────────────
    [BindProperty] public DateTime Desde         { get; set; }
    [BindProperty] public DateTime Hasta         { get; set; }
    [BindProperty] public string?  FiltroCliente { get; set; }
    [BindProperty] public string?  Letra         { get; set; }

    // ── Resultados ───────────────────────────────────────────────────────────
    public List<ComprobanteEmitidoDto> Comprobantes { get; private set; } = new();
    public EstadisticasFEDto           Estadisticas { get; private set; } = new();
    public bool                        Buscado      { get; private set; }

    // ── Carga inicial ────────────────────────────────────────────────────────
    public async Task OnGetAsync()
    {
        var hoy   = DateTime.Today;
        Desde     = new DateTime(hoy.Year, hoy.Month, 1);
        Hasta     = hoy;
        Estadisticas = await _service.GetEstadisticasAsync(Desde, Hasta);
    }

    // ── Buscar ───────────────────────────────────────────────────────────────
    public async Task OnPostBuscarAsync()
    {
        if (Hasta < Desde) Hasta = Desde;

        var filtros = new BuscarComprobantesDto
        {
            Desde         = Desde,
            Hasta         = Hasta,
            FiltroCliente = FiltroCliente,
            Letra         = string.IsNullOrWhiteSpace(Letra) ? null : Letra
        };

        Comprobantes = await _service.BuscarAsync(filtros);
        Estadisticas = await _service.GetEstadisticasAsync(Desde, Hasta);
        Buscado      = true;
    }

    // ── Detalle de comprobante (JSON) ────────────────────────────────────────
    public async Task<IActionResult> OnGetDetalleAsync(long id)
    {
        var detalle = await _service.GetDetalleAsync(id);
        if (detalle == null)
            return new JsonResult(new { ok = false, msg = "Comprobante no encontrado." });

        return new JsonResult(new { ok = true, detalle }, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
    }

    // ── Estadísticas por período (JSON, para actualización dinámica) ──────────
    public async Task<IActionResult> OnGetEstadisticasAsync(string desde, string hasta)
    {
        if (!DateTime.TryParse(desde, out var dDesde)) dDesde = DateTime.Today.AddMonths(-1);
        if (!DateTime.TryParse(hasta, out var dHasta)) dHasta = DateTime.Today;
        var stats = await _service.GetEstadisticasAsync(dDesde, dHasta);
        return new JsonResult(stats, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
    }
}
