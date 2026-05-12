using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Comercial_Web.Pages.Ventas.ReporteDevoluciones;

[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly IDevolucionService _devolucionService;
    private readonly IPedidoService     _pedidoService;
    private readonly IVentaService      _ventaService;

    public IndexModel(
        IDevolucionService devolucionService,
        IPedidoService     pedidoService,
        IVentaService      ventaService)
    {
        _devolucionService = devolucionService;
        _pedidoService     = pedidoService;
        _ventaService      = ventaService;
    }

    // ── Parámetros del sistema ────────────────────────────────────────────────
    public int HaceNotaVentaTK { get; private set; }

    // ── Carga inicial ─────────────────────────────────────────────────────────
    public async Task OnGetAsync()
    {
        var prm = await _ventaService.GetParametrosAsync(Environment.MachineName);
        HaceNotaVentaTK = prm.HaceNotaVentaTK;
    }

    // ── Buscar devoluciones (GET → JSON) ──────────────────────────────────────
    public async Task<IActionResult> OnGetBuscarAsync(
        string? desde, string? hasta, string? clientes)
    {
        if (!DateTime.TryParse(desde, out var dDesde)) dDesde = DateTime.Today.AddMonths(-1);
        if (!DateTime.TryParse(hasta, out var dHasta)) dHasta = DateTime.Today;

        var clienteIds = string.IsNullOrWhiteSpace(clientes)
            ? new List<int>()
            : clientes.Split(',')
                .Select(s => int.TryParse(s.Trim(), out var x) ? x : 0)
                .Where(x => x > 0)
                .ToList();

        var lista = await _devolucionService.BuscarDevolucionesAsync(dDesde, dHasta, clienteIds);

        return new JsonResult(lista, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    // ── Detalle de devolución (GET → JSON) ────────────────────────────────────
    public async Task<IActionResult> OnGetDetalleAsync(long id)
    {
        var dto = await _devolucionService.GetDevolucionParaImpresionAsync(id);
        if (dto == null)
            return new JsonResult(new { ok = false, msg = "Devolución no encontrada." });

        return new JsonResult(new { ok = true, detalle = dto }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    // ── Búsqueda de clientes para el multiselect ──────────────────────────────
    public async Task<IActionResult> OnGetBuscarClientesAsync(string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return new JsonResult(new List<object>());
        return new JsonResult(await _pedidoService.BuscarClientesAsync(q));
    }
}
