using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Comercial_Web.Pages.Ventas.Ventas;

[Authorize]
public class ComprobanteXModel : PageModel
{
    private readonly IVentaService    _ventaService;
    private readonly IParametroService _parametroService;

    public ComprobanteXModel(IVentaService ventaService, IParametroService parametroService)
    {
        _ventaService     = ventaService;
        _parametroService = parametroService;
    }

    // ── Datos para la vista ─────────────────────────────────────────────
    public VentaImpresionDto? Venta         { get; private set; }
    public string             LogoHtml      { get; private set; } = "";
    public string             EmpresaNombre { get; private set; } = "";
    public string             EmpresaDirec  { get; private set; } = "";
    public string             EmpresaTel    { get; private set; } = "";
    public string             EmpresaCuil   { get; private set; } = "";
    public string             Error         { get; private set; } = "";
    public bool               ImputaEnVenta { get; private set; }

    // Totales calculados
    public decimal TotPrecioBruto { get; private set; }
    public decimal TotSubtSIva  { get; private set; }
    public decimal TotIva       { get; private set; }
    public decimal TotImpuesto  { get; private set; }
    public decimal TotTotal     { get; private set; }

    public async Task<IActionResult> OnGetAsync(long id)
    {
        if (id <= 0) { Error = "ID de venta inválido."; return Page(); }

        Venta = await _ventaService.GetVentaParaImpresionAsync(id);
        if (Venta == null) { Error = "Venta no encontrada."; return Page(); }

        // Datos empresa
        EmpresaNombre = await _parametroService.ObtenerValorAsync("empresa", "nombre")    ?? "";
        EmpresaDirec  = await _parametroService.ObtenerValorAsync("empresa", "direccion") ?? "";
        EmpresaTel    = await _parametroService.ObtenerValorAsync("empresa", "telefono")  ?? "";
        EmpresaCuil   = await _parametroService.ObtenerValorAsync("empresa", "cuil")      ?? "";

        // Parámetro: mostrar detalle de formas de pago en el comprobante
        ImputaEnVenta = (await _parametroService.ObtenerValorAsync("ventas", "pagosEnVenta")) == "1";

        // Logo
        var logoParam = await _parametroService.GetLogoAsync();
        if (logoParam?.Imagen != null && logoParam.Imagen.Length > 0)
        {
            var img  = logoParam.Imagen;
            string mime = "image/png";
            if (img.Length > 1)
            {
                if      (img[0] == 0xFF && img[1] == 0xD8) mime = "image/jpeg";
                else if (img[0] == 0x89 && img[1] == 0x50) mime = "image/png";
                else if (img[0] == 0x47 && img[1] == 0x49) mime = "image/gif";
                else if (img[0] == 0x42 && img[1] == 0x4D) mime = "image/bmp";
            }
            LogoHtml = $"<img src=\"data:{mime};base64,{Convert.ToBase64String(img)}\" style=\"max-height:70px;max-width:180px;\" />";
        }

        // Calcular totales
        decimal ivaRate = Venta.Iva / 100m;
        foreach (var d in Venta.Detalle)
        {
            // desc/rec por línea
            decimal descPct = d.Recargo.HasValue && d.Recargo > 0  ? d.Recargo.Value
                            : d.Descuento.HasValue && d.Descuento > 0 ? -d.Descuento.Value
                            : 0m;
            // Si existe desc/rec global en la venta
            if (Venta.Descuento.HasValue)
                descPct = Venta.Descuento.Value * -1m + (Venta.Recargo ?? 0m);

            decimal sub      = d.PrecioSinIva * (1m + descPct / 100m);
            decimal conIva   = sub * (1m + ivaRate);
            decimal cant     = d.Cantidad;

            TotPrecioBruto += d.PrecioSinIva * cant;
            TotSubtSIva += sub * cant;
            TotIva      += (conIva - sub) * cant;
        }
        TotImpuesto = TotSubtSIva * (Venta.Impuesto / 100m);
        TotTotal    = TotSubtSIva + TotIva + TotImpuesto;

        return Page();
    }
}
