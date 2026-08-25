using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Comercial_Web.Pages.Ventas.Ventas;

[Authorize]
public class TicketXModel : PageModel
{
    private readonly IVentaService     _ventaService;
    private readonly IParametroService _parametroService;

    public TicketXModel(IVentaService ventaService, IParametroService parametroService)
    {
        _ventaService     = ventaService;
        _parametroService = parametroService;
    }

    // ── Datos para la vista ─────────────────────────────────────────────
    public VentaImpresionDto? Venta          { get; private set; }
    public string             EmpresaNombre  { get; private set; } = "";
    public string             EmpresaRazon   { get; private set; } = "";
    public string             EmpresaDirec   { get; private set; } = "";
    public string             EmpresaTel     { get; private set; } = "";
    public string             Error          { get; private set; } = "";
    public bool               ImputaEnVenta  { get; private set; }

    // Ancho del ticket en mm (usado en CSS @page y .ticket)
    public int AnchoMm { get; private set; } = 80;

    // Totales
    public decimal TotTotal { get; private set; }

    // Descuento general sobre Total S/IVA (modo bonificacionesPorDetalle = 1)
    public bool    EsDescuentoGeneral    { get; private set; }
    public decimal DescGeneralPct        { get; private set; }
    public decimal TotDescGeneralImporte { get; private set; }

    public async Task<IActionResult> OnGetAsync(long id)
    {
        if (id <= 0) { Error = "ID de venta inválido."; return Page(); }

        Venta = await _ventaService.GetVentaParaImpresionAsync(id);
        if (Venta == null) { Error = "Venta no encontrada."; return Page(); }

        // Parámetros empresa
        EmpresaNombre = await _parametroService.ObtenerValorAsync("empresa", "nombre")      ?? "";
        EmpresaRazon  = await _parametroService.ObtenerValorAsync("empresa", "razonSocial") ?? "";
        EmpresaDirec  = await _parametroService.ObtenerValorAsync("empresa", "direccion")   ?? "";
        EmpresaTel    = await _parametroService.ObtenerValorAsync("empresa", "telefono")    ?? "";

        // Ancho del ticket
        var anchoStr = await _parametroService.ObtenerValorAsync("ventas", "anchoTk") ?? "";
        if (int.TryParse(anchoStr, out var a) && a > 0) AnchoMm = a;

        // Parámetro: mostrar detalle de formas de pago en el ticket
        ImputaEnVenta = (await _parametroService.ObtenerValorAsync("ventas", "pagosEnVenta")) == "1";

        // Modo de bonificación: 1 = por línea (Venta.Descuento es descuento general sobre Total S/IVA)
        var bonStr = await _parametroService.ObtenerValorAsync("ventas", "bonificacionesPorDetalle");
        EsDescuentoGeneral = bonStr == "1";
        DescGeneralPct = (EsDescuentoGeneral && Venta.Descuento.HasValue) ? Venta.Descuento.Value : 0m;

        // Total (precio con iva * cantidad por línea, sin impuesto adicional)
        decimal ivaRate = Venta.Iva / 100m;
        decimal totSin = 0m;      // subtotal s/IVA bruto (por línea)
        decimal totConIva = 0m;   // subtotal c/IVA bruto (por línea)
        foreach (var d in Venta.Detalle)
        {
            decimal descPct = d.Recargo.HasValue && d.Recargo > 0   ?  d.Recargo.Value
                            : d.Descuento.HasValue && d.Descuento > 0 ? -d.Descuento.Value
                            : 0m;
            // Regla global clásica SOLO en modo global (no en descuento general)
            if (!EsDescuentoGeneral && Venta.Descuento.HasValue)
                descPct = Venta.Descuento.Value * -1m + (Venta.Recargo ?? 0m);

            decimal sub    = d.PrecioSinIva * (1m + descPct / 100m);
            decimal conIva = sub * (1m + ivaRate);
            totSin    += sub * d.Cantidad;
            totConIva += conIva * d.Cantidad;
        }

        // Descuento general: netea el total (no altera las líneas)
        decimal factor = (EsDescuentoGeneral && DescGeneralPct > 0) ? (1m - DescGeneralPct / 100m) : 1m;
        TotDescGeneralImporte = totSin * (1m - factor);
        totSin    *= factor;
        totConIva *= factor;
        TotTotal = totConIva + totSin * (Venta.Impuesto / 100m);

        return Page();
    }
}
