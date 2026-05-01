using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Comercial_Web.Pages.Facturacion.FacturacionLotes;

[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly IFacturacionLotesService        _service;
    private readonly IVentaService                   _ventaService;
    private readonly IFacturacionElectronicaService  _feService;

    public IndexModel(
        IFacturacionLotesService       service,
        IVentaService                  ventaService,
        IFacturacionElectronicaService feService)
    {
        _service    = service;
        _ventaService = ventaService;
        _feService  = feService;
    }

    // ── Filtros ─────────────────────────────────────────────────────────────
    [BindProperty] public DateTime Desde { get; set; }
    [BindProperty] public DateTime Hasta { get; set; }

    // ── Resultados ──────────────────────────────────────────────────────────
    public List<VentaNoFacturadaDto> Ventas  { get; private set; } = new();
    public bool                      Buscado { get; private set; }

    // ── Totalizadores ────────────────────────────────────────────────────────
    public int     TotalCantidad  => Ventas.Count;
    public decimal TotalImporte   => Ventas.Sum(v => v.TotalVenta);
    public decimal TotalIva       => Ventas.Sum(v => v.Iva);
    public decimal TotalDescuento => Ventas.Sum(v => v.Descuento);
    public decimal TotalRecargo   => Ventas.Sum(v => v.Recargo);
    public decimal TotalImpuesto  => Ventas.Sum(v => v.Impuesto);

    // ── Parámetros de impresión ──────────────────────────────────────────────
    public int HaceNotaVentaTK { get; private set; }
    public int AnchoTk         { get; private set; }

    // ── GET ──────────────────────────────────────────────────────────────────
    public async Task OnGetAsync()
    {
        Desde = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        Hasta = DateTime.Today;

        var p = await _ventaService.GetParametrosAsync(Environment.MachineName);
        HaceNotaVentaTK = p.HaceNotaVentaTK;
        AnchoTk         = p.AnchoTk;
    }

    // ── POST Buscar ──────────────────────────────────────────────────────────
    public async Task OnPostBuscarAsync()
    {
        Ventas  = await _service.GetVentasNoFacturadasAsync(Desde, Hasta);
        Buscado = true;

        var p = await _ventaService.GetParametrosAsync(Environment.MachineName);
        HaceNotaVentaTK = p.HaceNotaVentaTK;
        AnchoTk         = p.AnchoTk;
    }

    // ── GET Detalle de venta ─────────────────────────────────────────────────
    public async Task<IActionResult> OnGetVentaDetalleAsync(long id)
    {
        var dto = await _ventaService.GetVentaParaImpresionAsync(id);
        if (dto == null) return new JsonResult(new { ok = false });
        return new JsonResult(new { ok = true, venta = dto });
    }

    // ── GET Facturar una venta (llamado de forma secuencial por el JS) ────────
    public async Task<IActionResult> OnGetFacturarVentaAsync(long id)
    {
        try
        {
            var result = await _feService.EmitirFacturaVentaAsync(id);
            return new JsonResult(new
            {
                ok      = result.Ok,
                ventaId = id,
                cae     = result.Cae,
                numero  = result.NumeroComprobante,
                errores = result.Errores
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new
            {
                ok      = false,
                ventaId = id,
                cae     = (string?)null,
                numero  = (string?)null,
                errores = new[] { ex.Message }
            });
        }
    }
}
