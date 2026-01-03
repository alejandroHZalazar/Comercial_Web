// Pages/Estadisticas/Ranking/Index.cshtml.cs
using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Reporting.NETCore;

namespace Comercial_Web.Pages.Estadisticas.Ranking
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IVentasRankingService _rankingService;

        public IndexModel(IVentasRankingService rankingService)
        {
            _rankingService = rankingService;
        }

        [BindProperty] public DateTime Desde { get; set; } = DateTime.Today;
        [BindProperty] public DateTime Hasta { get; set; } = DateTime.Today;

        // Datos
        public List<RankingItemDto> Items { get; set; } = new();

        // Config de grilla reutilizable
        public string Tipo { get; set; } = "Clientes";      // "Clientes" o "Productos"
        public string ColCodigo { get; set; } = "Nro";
        public string ColNombre { get; set; } = "Cliente";
        public string ColValor { get; set; } = "Ventas";
        public string FormatoValor { get; set; } = "N2";     // N2 para ventas; para productos podés usar "N0" o tu cantStock

        public async Task OnGetAsync() { /* opcional: precarga */ }

        public async Task<IActionResult> OnPostRankingClientesAsync()
        {
            Tipo = "Clientes";
            ColCodigo = "Nro";
            ColNombre = "Cliente";
            ColValor = "Ventas";
            FormatoValor = "N2";

            Items = await _rankingService.TraerVentasRankingClientesAsync(Desde, Hasta);
            return Page();
        }

        public async Task<IActionResult> OnPostRankingProductosAsync()
        {
            Tipo = "Productos";
            ColCodigo = "Codigo Prov.";
            ColNombre = "Producto";
            ColValor = "Cantidad";
            FormatoValor = "N2"; // si querés usar cantStock: "N" + cantStock

            Items = await _rankingService.TraerVentasRankingProductosAsync(Desde, Hasta);
            return Page();
        }
       
    }
}