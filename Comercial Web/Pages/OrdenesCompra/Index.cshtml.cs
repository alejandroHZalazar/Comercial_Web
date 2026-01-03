using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Domain.Contracts;
using Microsoft.AspNetCore.Authorization;

namespace Comercial_Web.Pages.OrdenesCompra
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IProveedorService _proveedorService;
        private readonly ICondicionIvaService _ivaService;
        private readonly IParametroService _parametroService;
        private readonly IProductoService _productoService;

        public IndexModel(
            IProveedorService proveedorService,
            ICondicionIvaService ivaService,
            IParametroService parametroService,
            IProductoService productoService)
        {
            _proveedorService = proveedorService;
            _ivaService = ivaService;
            _parametroService = parametroService;
            _productoService = productoService;
        }

        public SelectList Proveedores { get; set; } = null!;
        public SelectList Ivas { get; set; } = null!;
        public bool Cargado { get; set; } = false;

        [BindProperty] public int? ProveedorId { get; set; }
        [BindProperty] public int? IvaId { get; set; }
        [BindProperty] public int TipoBusqueda { get; set; }
        [BindProperty] public decimal Descuento { get; set; } = 0;
        [BindProperty] public decimal Recargo { get; set; } = 0;
        public decimal CantidadStep { get; set; } = 1;

        public async Task OnGetAsync()
        {
            var proveedores = await _proveedorService.TraerCabeceraAsync();
            var ivas = await _ivaService.TraerPorcentajesAsync();
            TipoBusqueda = await _parametroService.ObtenerIndiceBusquedaNotaPedidoAsync();
            CantidadStep = await _productoService.ObtenerDecimalesStockAsync();

            Proveedores = new SelectList(proveedores, "Id", "NombreComercial");
            Ivas = new SelectList(ivas, "Id", "Valor");

            Cargado = true;
        }
    }
}