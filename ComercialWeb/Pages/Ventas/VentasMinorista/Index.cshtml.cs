using Application.Interfaces;
using Domain.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Comercial_Web.Pages.Ventas.VentasMinorista;

[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : Comercial_Web.Pages.Ventas.Ventas.IndexModel
{
    public int VendedorAdminId { get; private set; }

    public IndexModel(
        IVentaService         ventaService,
        IPedidoService        pedidoService,
        IUsuarioService       usuarioService,
        IClienteService       clienteService,
        ICondicionIvaService  condIvaService,
        ILocalidadService     localidadService,
        IZonaClienteService   zonaService,
        ICobroService         cobroService,
        IPorcentajeIvaService porcIvaService,
        IPromocionService     promoService,
        IParametroService     parametroService)
        : base(ventaService, pedidoService, usuarioService, clienteService,
               condIvaService, localidadService, zonaService, cobroService,
               porcIvaService, promoService, parametroService)
    { }

    protected override bool OmitirRedirectMinorista => true;

    public override async Task<IActionResult> OnGetAsync()
    {
        var result = await base.OnGetAsync();
        if (result is not PageResult) return result;

        var s = await _parametroService.ObtenerValorAsync("configuracion", "admin");
        VendedorAdminId = int.TryParse(s, out var id) ? id : 0;
        return Page();
    }
}
