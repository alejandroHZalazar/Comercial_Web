using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Comercial_Web.Pages.Ventas.Devolucion;

[Authorize]
public class TicketXModel : PageModel
{
    private readonly IDevolucionService _devolucionService;
    private readonly IParametroService  _parametroService;

    public TicketXModel(IDevolucionService devolucionService, IParametroService parametroService)
    {
        _devolucionService = devolucionService;
        _parametroService  = parametroService;
    }

    // ── Datos para la vista ─────────────────────────────────────────────
    public DevolucionImpresionDto? Devolucion    { get; private set; }
    public string                  EmpresaNombre { get; private set; } = "";
    public string                  EmpresaRazon  { get; private set; } = "";
    public string                  EmpresaDirec  { get; private set; } = "";
    public string                  EmpresaTel    { get; private set; } = "";
    public string                  Error         { get; private set; } = "";

    // Ancho del ticket en mm
    public int AnchoMm { get; private set; } = 80;

    // Total
    public decimal TotTotal { get; private set; }

    public async Task<IActionResult> OnGetAsync(long id)
    {
        if (id <= 0) { Error = "ID de devolución inválido."; return Page(); }

        Devolucion = await _devolucionService.GetDevolucionParaImpresionAsync(id);
        if (Devolucion == null) { Error = "Devolución no encontrada."; return Page(); }

        // Parámetros empresa
        EmpresaNombre = await _parametroService.ObtenerValorAsync("empresa", "nombre")      ?? "";
        EmpresaRazon  = await _parametroService.ObtenerValorAsync("empresa", "razonSocial") ?? "";
        EmpresaDirec  = await _parametroService.ObtenerValorAsync("empresa", "direccion")   ?? "";
        EmpresaTel    = await _parametroService.ObtenerValorAsync("empresa", "telefono")    ?? "";

        // Ancho del ticket
        var anchoStr = await _parametroService.ObtenerValorAsync("ventas", "anchoTk") ?? "";
        if (int.TryParse(anchoStr, out var a) && a > 0) AnchoMm = a;

        // Total
        decimal ivaRate = Devolucion.Iva / 100m;
        decimal totSin  = 0m;
        foreach (var det in Devolucion.Detalle)
        {
            decimal descPct = det.Recargo.HasValue && det.Recargo > 0   ?  det.Recargo.Value
                            : det.Descuento.HasValue && det.Descuento > 0 ? -det.Descuento.Value
                            : 0m;
            if (Devolucion.Descuento.HasValue)
                descPct = Devolucion.Descuento.Value * -1m + (Devolucion.Recargo ?? 0m);

            decimal sub    = det.PrecioSinIva * (1m + descPct / 100m);
            decimal conIva = sub * (1m + ivaRate);
            totSin    += sub * det.Cantidad;
            TotTotal  += conIva * det.Cantidad;
        }
        TotTotal += totSin * (Devolucion.Impuesto / 100m);

        return Page();
    }
}
