using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Comercial_Web.Pages.Ventas.Devolucion;

[Authorize]
public class ComprobanteXModel : PageModel
{
    private readonly IDevolucionService _devolucionService;
    private readonly IParametroService  _parametroService;

    public ComprobanteXModel(IDevolucionService devolucionService, IParametroService parametroService)
    {
        _devolucionService = devolucionService;
        _parametroService  = parametroService;
    }

    // ── Datos para la vista ─────────────────────────────────────────────
    public DevolucionImpresionDto? Devolucion    { get; private set; }
    public string                  LogoHtml      { get; private set; } = "";
    public string                  EmpresaNombre { get; private set; } = "";
    public string                  EmpresaDirec  { get; private set; } = "";
    public string                  EmpresaTel    { get; private set; } = "";
    public string                  EmpresaCuil   { get; private set; } = "";
    public string                  Error         { get; private set; } = "";

    // Totales calculados
    public decimal TotPrecioBruto { get; private set; }
    public decimal TotSubtSIva    { get; private set; }
    public decimal TotIva         { get; private set; }
    public decimal TotImpuesto    { get; private set; }
    public decimal TotTotal       { get; private set; }

    public async Task<IActionResult> OnGetAsync(long id)
    {
        if (id <= 0) { Error = "ID de devolución inválido."; return Page(); }

        Devolucion = await _devolucionService.GetDevolucionParaImpresionAsync(id);
        if (Devolucion == null) { Error = "Devolución no encontrada."; return Page(); }

        // Datos empresa
        EmpresaNombre = await _parametroService.ObtenerValorAsync("empresa", "nombre")    ?? "";
        EmpresaDirec  = await _parametroService.ObtenerValorAsync("empresa", "direccion") ?? "";
        EmpresaTel    = await _parametroService.ObtenerValorAsync("empresa", "telefono")  ?? "";
        EmpresaCuil   = await _parametroService.ObtenerValorAsync("empresa", "cuil")      ?? "";

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
        decimal ivaRate = Devolucion.Iva / 100m;
        foreach (var d in Devolucion.Detalle)
        {
            decimal descPct = d.Recargo.HasValue && d.Recargo > 0   ?  d.Recargo.Value
                            : d.Descuento.HasValue && d.Descuento > 0 ? -d.Descuento.Value
                            : 0m;
            if (Devolucion.Descuento.HasValue)
                descPct = Devolucion.Descuento.Value * -1m + (Devolucion.Recargo ?? 0m);

            decimal sub    = d.PrecioSinIva * (1m + descPct / 100m);
            decimal conIva = sub * (1m + ivaRate);
            decimal cant   = d.Cantidad;

            TotPrecioBruto += d.PrecioSinIva * cant;
            TotSubtSIva    += sub * cant;
            TotIva         += (conIva - sub) * cant;
        }
        TotImpuesto = TotSubtSIva * (Devolucion.Impuesto / 100m);
        TotTotal    = TotSubtSIva + TotIva + TotImpuesto;

        return Page();
    }
}
