using Application.Interfaces;
using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Comercial_Web.Pages.Ventas.ReportesVentas;

[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly IReporteVentasService _reporteService;
    private readonly IClienteService       _clienteService;
    private readonly IVentaService         _ventaService;

    public IndexModel(
        IReporteVentasService reporteService,
        IClienteService       clienteService,
        IVentaService         ventaService)
    {
        _reporteService = reporteService;
        _clienteService = clienteService;
        _ventaService   = ventaService;
    }

    public int HaceNotaVentaTK { get; private set; }
    public int AnchoTk         { get; private set; }

    public async Task OnGetAsync()
    {
        var p = await _ventaService.GetParametrosAsync(Environment.MachineName);
        HaceNotaVentaTK = p.HaceNotaVentaTK;
        AnchoTk         = p.AnchoTk;
    }

    public async Task<List<SelectListItem>> GetClientesListAsync()
    {
        var todos = await _clienteService.GetAllAsync();
        return todos
            .Where(c => c.Baja != true)
            .OrderBy(c => c.NombreComercial)
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.NombreComercial ?? "" })
            .ToList();
    }

    public async Task<IActionResult> OnGetBuscarVentasAsync(
        string? desde, string? hasta, string? clientes)
    {
        DateTime? dDesde = null, dHasta = null;
        if (DateTime.TryParse(desde, out var d)) dDesde = d;
        if (DateTime.TryParse(hasta, out var h)) dHasta = h;

        var listCli = string.IsNullOrWhiteSpace(clientes) ? new List<int>()
            : clientes.Split(',')
                .Select(v => int.TryParse(v.Trim(), out var x) ? x : 0)
                .Where(x => x > 0).ToList();

        var lista = await _reporteService.BuscarVentasAsync(dDesde, dHasta, listCli);
        return new JsonResult(lista);
    }

    public async Task<IActionResult> OnGetVentaDetalleAsync(long id)
    {
        var dto = await _ventaService.GetVentaParaImpresionAsync(id);
        if (dto == null) return new JsonResult(new { ok = false });
        return new JsonResult(new { ok = true, venta = dto });
    }
}
