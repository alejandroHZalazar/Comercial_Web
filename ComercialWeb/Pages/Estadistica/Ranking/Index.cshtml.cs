using Application.Interfaces;
using Comercial_Web.Helpers;
using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Comercial_Web.Pages.Estadisticas.Ranking
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IVentasRankingService _rankingService;
        private readonly IProveedorService _proveedorService;

        public IndexModel(IVentasRankingService rankingService, IProveedorService proveedorService)
        {
            _rankingService = rankingService;
            _proveedorService = proveedorService;
        }

        [BindProperty] public DateTime Desde { get; set; } = DateTime.Today;
        [BindProperty] public DateTime Hasta { get; set; } = DateTime.Today;
        [BindProperty] public List<int> ProveedoresSeleccionados { get; set; } = new();

        // Datos grillas
        public List<RankingProductoDetalleDto> ItemsProductos { get; set; } = new();
        public List<RankingClienteDetalleDto> ItemsClientes { get; set; } = new();

        // Tipo activo
        public string TipoActivo { get; set; } = "";

        // Combo proveedores
        public List<SelectListItem> Proveedores { get; set; } = new();

        public async Task OnGetAsync()
        {
            await CargarProveedoresAsync();
        }

        public async Task<IActionResult> OnPostRankingClientesAsync()
        {
            await CargarProveedoresAsync();
            TipoActivo = "Clientes";

            var ids = ProveedoresSeleccionados.Any() ? ProveedoresSeleccionados : null;
            ItemsClientes = await _rankingService.TraerRankingClientesDetalleAsync(Desde, Hasta, ids);

            return Page();
        }

        public async Task<IActionResult> OnPostRankingProductosAsync()
        {
            await CargarProveedoresAsync();
            TipoActivo = "Productos";

            var ids = ProveedoresSeleccionados.Any() ? ProveedoresSeleccionados : null;
            ItemsProductos = await _rankingService.TraerRankingProductosDetalleAsync(Desde, Hasta, ids);

            return Page();
        }

        // ====== EXPORTAR CSV ======
        public async Task<IActionResult> OnPostExportarClientesCsvAsync()
        {
            var ids = ProveedoresSeleccionados.Any() ? ProveedoresSeleccionados : null;
            var datos = await _rankingService.TraerRankingClientesDetalleAsync(Desde, Hasta, ids);

            var columnas = new Dictionary<string, string>
            {
                { "Cliente", "Cliente" },
                { "TotalSinIva", "Total Sin IVA" },
                { "TicketPromedio", "Ticket Promedio" },
                { "CantCompras", "Cant Compras" },
                { "ProdDistintos", "Prod Distintos" },
                { "Participacion", "% Participación" },
                { "Costo", "Costo" },
                { "Rentabilidad", "Rentabilidad" },
                { "Margen", "% Margen" }
            };

            var csv = CsvExporter.Generar(datos, columnas);
            return File(csv, "text/csv", $"RankingClientes_{Desde:yyyyMMdd}_{Hasta:yyyyMMdd}.csv");
        }

        public async Task<IActionResult> OnPostExportarProductosCsvAsync()
        {
            var ids = ProveedoresSeleccionados.Any() ? ProveedoresSeleccionados : null;
            var datos = await _rankingService.TraerRankingProductosDetalleAsync(Desde, Hasta, ids);

            var columnas = new Dictionary<string, string>
            {
                { "Proveedor", "Proveedor" },
                { "CodProv", "Código Prov." },
                { "Producto", "Producto" },
                { "Cantidad", "Cantidad" },
                { "TotalSinIva", "Total Sin IVA" },
                { "PrecioPromedio", "Precio Promedio" },
                { "Participacion", "% Participación" },
                { "Costo", "Costo" },
                { "Rentabilidad", "Rentabilidad" },
                { "CantidadVentas", "Cant. Ventas" }
            };

            var csv = CsvExporter.Generar(datos, columnas);
            return File(csv, "text/csv", $"RankingProductos_{Desde:yyyyMMdd}_{Hasta:yyyyMMdd}.csv");
        }

        // ====== Helpers ======
        private async Task CargarProveedoresAsync()
        {
            var proveedores = await _proveedorService.GetAllAsync();
            Proveedores = proveedores
                .Select(p => new SelectListItem(p.NombreComercial, p.Id.ToString()))
                .ToList();
        }
    }
}
