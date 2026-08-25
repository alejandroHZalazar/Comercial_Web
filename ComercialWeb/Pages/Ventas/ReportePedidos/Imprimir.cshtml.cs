using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Comercial_Web.Pages.Ventas.ReportePedidos;

[Authorize]
public class ImprimirModel : PageModel
{
    private readonly IPedidoService    _pedidoService;
    private readonly IParametroService _parametroService;

    public ImprimirModel(IPedidoService pedidoService, IParametroService parametroService)
    {
        _pedidoService    = pedidoService;
        _parametroService = parametroService;
    }

    public PedidoCabeceraDto?          Cabecera       { get; private set; }
    public List<PedidoDetalleItemDto>  Detalle        { get; private set; } = new();
    public string                      LogoHtml       { get; private set; } = "";
    public string                      EmpresaNombre  { get; private set; } = "";
    public string                      EmpresaDirec   { get; private set; } = "";
    public string                      EmpresaTel     { get; private set; } = "";
    public string                      Error          { get; private set; } = "";

    // Calculated totals (server-side)
    public decimal TotPrecioSIva    { get; private set; }
    public decimal TotDescRec       { get; private set; }
    public decimal TotSubtSIva      { get; private set; }
    public decimal TotIva           { get; private set; }
    public decimal TotTotal         { get; private set; }

    // Descuento general sobre Total S/IVA (modo bonificacionesPorDetalle = 1)
    public bool    EsDescuentoGeneral    { get; private set; }
    public decimal DescGeneralPct        { get; private set; }
    public decimal TotDescGeneralImporte { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (id <= 0) { Error = "ID de pedido inválido."; return Page(); }

        Cabecera = await _pedidoService.GetCabeceraAsync(id);
        if (Cabecera == null) { Error = "Pedido no encontrado."; return Page(); }

        Detalle = await _pedidoService.GetDetalleAsync(id);

        // Company data
        EmpresaNombre = await _parametroService.ObtenerValorAsync("empresa", "nombre")    ?? "";
        EmpresaDirec  = await _parametroService.ObtenerValorAsync("empresa", "direccion") ?? "";
        EmpresaTel    = await _parametroService.ObtenerValorAsync("empresa", "telefono")  ?? "";

        // Logo
        var logoParam = await _parametroService.GetLogoAsync();
        if (logoParam?.Imagen != null && logoParam.Imagen.Length > 0)
        {
            string mime = "image/png";
            var img = logoParam.Imagen;
            if (img.Length > 1)
            {
                if      (img[0] == 0xFF && img[1] == 0xD8) mime = "image/jpeg";
                else if (img[0] == 0x89 && img[1] == 0x50) mime = "image/png";
                else if (img[0] == 0x47 && img[1] == 0x49) mime = "image/gif";
                else if (img[0] == 0x42 && img[1] == 0x4D) mime = "image/bmp";
            }
            LogoHtml = $"<img src=\"data:{mime};base64,{Convert.ToBase64String(img)}\" style=\"max-height:60px;max-width:160px;\" />";
        }

        // Modo de bonificación: 1 = por línea (cab.Descuento es descuento general sobre Total S/IVA)
        var bonStr = await _parametroService.ObtenerValorAsync("ventas", "bonificacionesPorDetalle");
        EsDescuentoGeneral = bonStr == "1";

        // Calculate totals applying the global descuento rule
        decimal ivaRate = (Cabecera.Iva ?? 0) / 100m;
        decimal? descRecGlobal = null;
        if (EsDescuentoGeneral)
            DescGeneralPct = Cabecera.Descuento ?? 0m;                       // descuento general (no reemplaza líneas)
        else if (Cabecera.Descuento.HasValue)
            descRecGlobal = (Cabecera.Descuento ?? 0m) * -1m + (Cabecera.Recargo ?? 0m);  // regla global clásica

        foreach (var l in Detalle)
        {
            decimal descRecPct  = descRecGlobal.HasValue
                ? descRecGlobal.Value
                : (l.Descuento * -1m + l.Recargo);
            decimal precioSIva   = l.PrecioSinIva;
            decimal subtSIva     = precioSIva * (1m + descRecPct / 100m);
            decimal precioConIva = subtSIva * (1m + ivaRate);
            decimal cant         = l.Cantidad;

            TotPrecioSIva += precioSIva * cant;
            TotDescRec    += (subtSIva - precioSIva) * cant;
            TotSubtSIva   += subtSIva * cant;
            TotIva        += (precioConIva - subtSIva) * cant;
            TotTotal      += precioConIva * cant;
        }

        // Descuento general: netea Total S/IVA, IVA y Total (no altera las líneas)
        if (EsDescuentoGeneral && DescGeneralPct > 0)
        {
            decimal factor = 1m - DescGeneralPct / 100m;
            TotDescGeneralImporte = TotSubtSIva * DescGeneralPct / 100m;
            TotSubtSIva *= factor;
            TotIva      *= factor;
            TotTotal    *= factor;
        }

        return Page();
    }

    // Helper: resolve descRec for a single line (used in view)
    public decimal GetDescRec(PedidoDetalleItemDto l)
    {
        // En modo por línea el desc/rec es siempre el de la línea (cab.Descuento es general)
        if (!EsDescuentoGeneral && Cabecera?.Descuento.HasValue == true)
            return (Cabecera.Descuento ?? 0m) * -1m + (Cabecera.Recargo ?? 0m);
        return l.Descuento * -1m + l.Recargo;
    }
}
