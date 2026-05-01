using Comercial_Web.Helpers;
using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Comercial_Web.Pages.Estadisticas.ExportarVentas;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IVentasExportarService _service;

    public IndexModel(IVentasExportarService service) => _service = service;

    [BindProperty] public DateTime Desde { get; set; } = DateTime.Today.AddDays(-30);
    [BindProperty] public DateTime Hasta { get; set; } = DateTime.Today;

    public List<VentaResumenExportarDto> ItemsResumen { get; private set; } = new();
    public List<VentaDetalleExportarDto> ItemsDetalle { get; private set; } = new();
    public string TipoActivo { get; private set; } = "";

    public void OnGet() { }

    public async Task<IActionResult> OnPostResumenAsync()
    {
        ItemsResumen = await _service.GetResumenAsync(Desde, Hasta);
        TipoActivo = "Resumen";
        return Page();
    }

    public async Task<IActionResult> OnPostDetalleAsync()
    {
        ItemsDetalle = await _service.GetDetalleAsync(Desde, Hasta);
        TipoActivo = "Detalle";
        return Page();
    }

    public async Task<IActionResult> OnPostExportarResumenCsvAsync()
    {
        var datos = await _service.GetResumenAsync(Desde, Hasta);

        var columnas = new Dictionary<string, string>
        {
            { "Nro",        "Nro" },
            { "Fecha",      "Fecha" },
            { "Total",      "Total S/IVA" },
            { "Costo",      "Costo" },
            { "Cliente",    "Cliente" },
            { "Cajero",     "Cajero" },
            { "Iva",        "IVA %" },
            { "Descuento",  "Descuento %" },
            { "Recargo",    "Recargo %" },
            { "Vendedor",   "Vendedor" },
            { "Comision",   "Comision" },
            { "Impuesto",   "Impuesto" },
            { "MedioPago1", "Medio Pago 1" },
            { "MedioPago2", "Medio Pago 2" },
            { "MedioPago3", "Medio Pago 3" }
        };

        var csv = CsvExporter.Generar(datos, columnas);
        return File(csv, "text/csv", $"VentasResumen_{Desde:yyyyMMdd}_{Hasta:yyyyMMdd}.csv");
    }

    public async Task<IActionResult> OnPostExportarDetalleCsvAsync()
    {
        var datos = await _service.GetDetalleAsync(Desde, Hasta);

        var columnas = new Dictionary<string, string>
        {
            { "Venta",        "Venta" },
            { "Fecha",        "Fecha" },
            { "Cliente",      "Cliente" },
            { "Proveedor",    "Proveedor" },
            { "CodProveedor", "Cod Proveedor" },
            { "Producto",     "Producto" },
            { "Precio",       "Precio" },
            { "Descuento",    "Descuento" },
            { "Recargo",      "Recargo" },
            { "PrecioSinIva", "Precio S/IVA" },
            { "Cantidad",     "Cantidad" },
            { "Costo",        "Costo" },
            { "Subtotal",     "Subtotal" }
        };

        var csv = CsvExporter.Generar(datos, columnas);
        return File(csv, "text/csv", $"VentasDetalle_{Desde:yyyyMMdd}_{Hasta:yyyyMMdd}.csv");
    }
}
