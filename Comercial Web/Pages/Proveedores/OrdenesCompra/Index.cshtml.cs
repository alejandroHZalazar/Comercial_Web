using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Domain.Contracts;
using Microsoft.AspNetCore.Authorization;
using Domain.DTO;
using Domain.Entities;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Comercial_Web.Pages.Proveedores.OrdenesCompra
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly IProveedorService _proveedorService;
        private readonly IPorcentajeIvaService _ivaService;
        private readonly IParametroService _parametroService;
        private readonly IProductoService _productoService;
        private readonly IOrdenCompraService _ordenCompraService;
        private readonly ComercialDbContext _context;

        public IndexModel(
            IProveedorService proveedorService,
            IPorcentajeIvaService ivaService,
            IParametroService parametroService,
            IProductoService productoService,
            IOrdenCompraService ordenCompraService,
            ComercialDbContext context)
        {
            _proveedorService     = proveedorService;
            _ivaService           = ivaService;
            _parametroService     = parametroService;
            _productoService      = productoService;
            _ordenCompraService   = ordenCompraService;
            _context              = context;
        }

        public SelectList Proveedores { get; set; } = null!;
        public List<ProductoDto> ProductosProveedor { get; set; } = new();
        public List<SelectListItem> Ivas { get; set; } = new();
        public bool Cargado { get; set; } = false;
        public decimal CantidadStep { get; set; } = 1;

        [BindProperty] public int? ProveedorId { get; set; }
        [BindProperty] public int? IvaId { get; set; }
        [BindProperty] public int TipoBusqueda { get; set; }
        [BindProperty] public decimal Descuento { get; set; } = 0;
        [BindProperty] public decimal Recargo { get; set; } = 0;
        [BindProperty] public int? ProductoSeleccionado { get; set; }
        [BindProperty] public List<OrdenCompraDetalle> Detalles { get; set; } = new();
        [BindProperty] public decimal TotalGeneral { get; set; }
        [BindProperty] public decimal IvaValor { get; set; } = 0;
        [BindProperty] public bool ProveedorConfirmado { get; set; } = false;

        // ----------------------------------------------------------------
        public async Task OnGetAsync()
        {
            var proveedores = await _proveedorService.TraerCabeceraAsync();
            var ivas        = await _ivaService.GetAllAsync();
            TipoBusqueda    = await _parametroService.ObtenerIndiceBusquedaNotaPedidoAsync();
            CantidadStep    = await _parametroService.ObtenerCantidadDecimalesStockAsync();

            Proveedores = new SelectList(proveedores, "Id", "NombreComercial");

            Ivas = ivas.Select(i => new SelectListItem
            {
                Value = i.Valor.GetValueOrDefault().ToString(CultureInfo.InvariantCulture),
                Text  = i.Valor.GetValueOrDefault().ToString("0.###", CultureInfo.CurrentCulture)
            }).ToList();

            if (IvaId.HasValue)
            {
                var iva = await _ivaService.GetByIdAsync(IvaId.Value);
                IvaValor = iva?.Valor ?? 0m;
            }

            Cargado = true;
        }

        // ----------------------------------------------------------------
        public async Task<IActionResult> OnGetProductosAsync(int proveedorId)
        {
            var productos    = await _productoService.TraerProductosProveedorAsync(proveedorId);
            ProductosProveedor = productos;
            var descripciones = productos
                .Where(p => !string.IsNullOrWhiteSpace(p.Descripcion))
                .Select(p => p.Descripcion)
                .Distinct()
                .ToList();
            return new JsonResult(descripciones);
        }

        public async Task<IActionResult> OnGetBuscarProductoAsync(string filtro, int tipoBusqueda, int proveedorId)
        {
            ProductoLineaOCDto? producto = null;

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                if (tipoBusqueda == 1)
                    producto = await _ordenCompraService.BuscarPorCodProveedorOCAsync(filtro, proveedorId);
                else if (tipoBusqueda == 2)
                    producto = await _ordenCompraService.BuscarPorCodBarrasOCAsync(filtro, proveedorId);
                else
                    producto = await _ordenCompraService.TraerPorIdOCAsync(int.Parse(filtro));
            }

            if (producto != null)
                return new JsonResult(new
                {
                    encontrado    = true,
                    id            = producto.Id,
                    codProveedor  = producto.CodProveedor,
                    codBarras     = producto.CodBarras,
                    descripcion   = producto.Descripcion,
                    cantidad      = producto.Cantidad,
                    cantidadMinima= producto.CantidadMinima,
                    precio        = producto.PrecioProveedor
                });

            return new JsonResult(new { encontrado = false, mensaje = $"No existe el producto: {filtro}" });
        }

        public async Task<IActionResult> OnGetBuscarPorDescripcionAsync(string descripcion, int proveedorId)
        {
            if (string.IsNullOrWhiteSpace(descripcion))
                return new JsonResult(new { encontrado = false, mensaje = "Debe ingresar una descripción válida." });

            var producto = await _ordenCompraService.BuscarPorDescripcionExactaAsync(descripcion, proveedorId);

            if (producto != null)
                return new JsonResult(new
                {
                    encontrado    = true,
                    id            = producto.Id,
                    codProveedor  = producto.CodProveedor,
                    codBarras     = producto.CodBarras,
                    descripcion   = producto.Descripcion,
                    cantidad      = producto.Cantidad,
                    cantidadMinima= producto.CantidadMinima,
                    precio        = producto.PrecioProveedor
                });

            return new JsonResult(new { encontrado = false, mensaje = $"No existe el producto: {descripcion}" });
        }

        public async Task<IActionResult> OnGetRecalcularTotalesAsync(int ivaId)
        {
            CalcularTotales(IvaValor);
            return Partial("_Totales", TotalGeneral);
        }

        public async Task<JsonResult> OnGetCantMinimaAsync(int proveedorId)
        {
            var productos = await _ordenCompraService.TraerCantMinPorProveedorAsync(proveedorId);
            return new JsonResult(productos);
        }

        public async Task<JsonResult> OnGetProductosAPedirAsync(int proveedorId, DateTime desde, DateTime hasta)
        {
            var productos = await _ordenCompraService.TraerListaProdAPedirAsync(proveedorId, desde, hasta);
            return new JsonResult(productos);
        }

        private void CalcularTotales(decimal ivaValor)
        {
            decimal total = 0;
            foreach (var fila in Detalles)
            {
                var costoOrig = fila.PrecioProveedor ?? 0;
                decimal precioSinIva = costoOrig;
                if (Descuento > 0) { precioSinIva = Math.Round((100 - Descuento) * costoOrig / 100, 2); Recargo = 0; }
                else if (Recargo > 0) { precioSinIva = Math.Round((100 + Recargo) * costoOrig / 100, 2); Descuento = 0; }
                var precioConIva = Math.Round(precioSinIva * (1 + ivaValor / 100), 2);
                fila.Subtotal = Math.Round(precioConIva * (fila.Cantidad ?? 0), 2);
                total += fila.Subtotal ?? 0;
            }
            TotalGeneral = Math.Round(total, 2);
        }

        // ----------------------------------------------------------------
        public async Task<IActionResult> OnPostGrabarAsync()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var request = JsonSerializer.Deserialize<OrdenCompraDTO>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (request == null || request.Detalles == null || !request.Detalles.Any(d => d.Cantidad > 0))
                return BadRequest("Debe ingresar al menos un pedido válido.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var cabecera = new OrdenCompra
                {
                    Fecha       = DateTime.Now,
                    FkProveedor = request.ProveedorId,
                    Total       = request.Total,
                    Procesado   = false,
                    Iva         = request.Iva,
                    Recargo     = request.Recargo,
                    Descuento   = request.Descuento
                };
                _context.OrdenCompras.Add(cabecera);
                await _context.SaveChangesAsync();

                foreach (var d in request.Detalles.Where(x => x.Cantidad > 0))
                {
                    d.FkOrdenCompra = cabecera.Id;
                    d.Procesado     = false;
                    _context.OrdenCompraDetalles.Add(d);
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new JsonResult(new { success = true, id = cabecera.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest($"Error al grabar: {ex.Message}");
            }
        }

        // ----------------------------------------------------------------
        // REPORTE ORDEN DE COMPRA (reemplaza ReportViewerCore)
        // ----------------------------------------------------------------
        public async Task<IActionResult> OnGetReporteOrdenAsync(int id, string formato)
        {
            var datos = await _ordenCompraService.OrdenCompraImprimirAsync(id);
            if (!datos.Any())
                return Content("<p>Sin datos para la orden N° " + id + "</p>", "text/html");

            var cab       = datos.First();
            var provId    = cab.fk_proveedor ?? 0;
            var cantDec   = await _parametroService.ObtenerCantidadDecimalesProductosAsync();
            var cantStock = await _parametroService.ObtenerCantidadDecimalesStockAsync();

            // Empresa
            var empNombre  = await _parametroService.ObtenerValorAsync("empresa", "nombre")      ?? "";
            var empRazon   = await _parametroService.ObtenerValorAsync("empresa", "razonSocial") ?? "";
            var empTel     = await _parametroService.ObtenerValorAsync("empresa", "telefono")    ?? "";
            var empEmail   = await _parametroService.ObtenerValorAsync("empresa", "email")       ?? "";

            // Logo → base64
            string logoHtml = "";
            var logoParam = await _parametroService.GetLogoAsync();
            if (logoParam?.Imagen != null && logoParam.Imagen.Length > 0)
            {
                string mime = "image/png";
                var img = logoParam.Imagen;
                if (img.Length > 1)
                {
                    if (img[0] == 0xFF && img[1] == 0xD8)           mime = "image/jpeg";
                    else if (img[0] == 0x42 && img[1] == 0x4D)      mime = "image/bmp";
                    else if (img[0] == 0x47 && img[1] == 0x49)      mime = "image/gif";
                }
                var b64 = Convert.ToBase64String(img);
                logoHtml = $"<img src=\"data:{mime};base64,{b64}\" style=\"height:55px;max-width:130px;object-fit:contain;\" />";
            }

            // Proveedor detalle
            var prov     = provId > 0 ? await _proveedorService.GetByIdAsync(provId) : null;
            var provNombre  = prov?.NombreComercial ?? cab.nombreComercial ?? "";
            var provDir     = prov?.Direccion       ?? cab.direccion        ?? "";
            var provTel     = prov?.Telefono        ?? prov?.Celular        ?? "";
            var provCuit    = prov?.Cuil            ?? "";

            var fechaEmision = cab.fecha?.ToString("dd/MM/yyyy") ?? DateTime.Now.ToString("dd/MM/yyyy");
            var nroPedido    = cab.id ?? id.ToString().PadLeft(8, '0');
            var fmtDec       = "N" + cantDec;
            var fmtQ         = "N" + cantStock;

            // Filas de detalle
            var sbRows = new StringBuilder();
            foreach (var r in datos)
            {
                sbRows.Append("<tr>");
                sbRows.Append($"<td>{HE(r.codBarras)}</td>");
                sbRows.Append($"<td>{HE(r.codProveedor)}</td>");
                sbRows.Append($"<td>{HE(r.descripcion)}</td>");
                sbRows.Append($"<td class=\"r\">{(r.precioProveedor ?? 0).ToString(fmtDec, CultureInfo.CurrentCulture)}</td>");
                sbRows.Append($"<td class=\"r\">{(r.cantidad ?? 0).ToString(fmtQ, CultureInfo.CurrentCulture)}</td>");
                sbRows.Append($"<td class=\"r\">{(r.subtotal ?? 0).ToString(fmtDec, CultureInfo.CurrentCulture)}</td>");
                sbRows.Append("</tr>");
            }

            var total = datos.Sum(r => r.subtotal ?? 0);
            var totalStr = total.ToString(fmtDec, CultureInfo.CurrentCulture);
            var fechaGen = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            var esExcel = formato.ToLower() == "excel";

            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"UTF-8\">");
            html.AppendLine($"<title>Nota de Pedido N° {nroPedido}</title>");
            html.AppendLine("<style>");
            html.AppendLine("*{box-sizing:border-box;margin:0;padding:0;}");
            html.AppendLine("body{font-family:Arial,Helvetica,sans-serif;font-size:9pt;color:#222;padding:10mm;}");
            html.AppendLine(".no-print{padding:6px;background:#f0f4ff;display:flex;gap:8px;margin-bottom:8mm;}");
            html.AppendLine(".no-print button{background:#4e73df;color:#fff;border:none;border-radius:6px;padding:5px 16px;font-size:9pt;cursor:pointer;}");
            html.AppendLine(".no-print .btn-sec{background:#858796;}");
            html.AppendLine(".doc-header{display:flex;justify-content:space-between;align-items:flex-start;padding-bottom:5mm;border-bottom:2px solid #1a2a4a;margin-bottom:5mm;}");
            html.AppendLine(".empresa-col{display:flex;align-items:flex-start;gap:4mm;}");
            html.AppendLine(".empresa-text h2{font-size:11pt;color:#1a2a4a;margin-bottom:2px;}");
            html.AppendLine(".empresa-text p{font-size:8pt;color:#555;line-height:1.6;margin:0;}");
            html.AppendLine(".pedido-col{text-align:right;}");
            html.AppendLine(".pedido-num{font-size:13pt;font-weight:700;color:#1a2a4a;letter-spacing:.5px;}");
            html.AppendLine(".pedido-fecha{font-size:8.5pt;color:#555;margin-top:3px;}");
            html.AppendLine(".prov-box{border:1px solid #d0d7e8;border-radius:4px;padding:3mm 5mm;margin-bottom:5mm;background:#f8f9fc;}");
            html.AppendLine(".prov-box h3{font-size:8pt;color:#4e73df;text-transform:uppercase;letter-spacing:.5px;margin-bottom:3px;}");
            html.AppendLine(".prov-box p{font-size:8.5pt;line-height:1.6;margin:0;}");
            html.AppendLine("table{width:100%;border-collapse:collapse;margin-bottom:3mm;}");
            html.AppendLine("thead tr{background:#1a2a4a;}");
            html.AppendLine("th{padding:2.5mm 3mm;color:#fff;font-size:7.5pt;text-transform:uppercase;letter-spacing:.3px;text-align:left;}");
            html.AppendLine("th.r,td.r{text-align:right;}");
            html.AppendLine("td{padding:2mm 3mm;font-size:8.5pt;border-bottom:1px solid #eee;}");
            html.AppendLine("tbody tr:nth-child(even){background:#f5f8ff;}");
            html.AppendLine(".total-row td{font-weight:700;font-size:10pt;color:#1a2a4a;border-top:2px solid #1a2a4a;padding-top:3mm;background:#eef2ff;}");
            html.AppendLine(".doc-foot{margin-top:6mm;font-size:7.5pt;color:#aaa;text-align:center;border-top:1px solid #eee;padding-top:2mm;}");
            html.AppendLine("@page{size:A4;margin:8mm;}");
            html.AppendLine("@media print{body{padding:0;}.no-print{display:none!important;}}");
            html.AppendLine("</style></head><body>");

            if (!esExcel)
            {
                html.AppendLine("<div class=\"no-print\">");
                html.AppendLine("  <button onclick=\"window.print()\">🖨 Imprimir / Guardar PDF</button>");
                html.AppendLine("  <button class=\"btn-sec\" onclick=\"window.close()\">✖ Cerrar</button>");
                html.AppendLine("</div>");
            }

            // Header
            html.AppendLine("<div class=\"doc-header\">");
            html.AppendLine("  <div class=\"empresa-col\">");
            if (!string.IsNullOrEmpty(logoHtml)) html.AppendLine("    <div>" + logoHtml + "</div>");
            html.AppendLine("    <div class=\"empresa-text\">");
            html.AppendLine($"      <h2>{HE(empNombre)}</h2>");
            if (!string.IsNullOrEmpty(empRazon))  html.AppendLine($"      <p>{HE(empRazon)}</p>");
            if (!string.IsNullOrEmpty(empTel))    html.AppendLine($"      <p>Tel: {HE(empTel)}</p>");
            if (!string.IsNullOrEmpty(empEmail))  html.AppendLine($"      <p>Email: {HE(empEmail)}</p>");
            html.AppendLine("    </div>");
            html.AppendLine("  </div>");
            html.AppendLine("  <div class=\"pedido-col\">");
            html.AppendLine("    <div class=\"pedido-num\">NOTA DE PEDIDO</div>");
            html.AppendLine($"   <div class=\"pedido-num\">N° {HE(nroPedido)}</div>");
            html.AppendLine($"   <div class=\"pedido-fecha\">Fecha de emisión: {fechaEmision}</div>");
            html.AppendLine("  </div>");
            html.AppendLine("</div>");

            // Proveedor
            html.AppendLine("<div class=\"prov-box\">");
            html.AppendLine("  <h3>Datos del Proveedor</h3>");
            html.AppendLine($"  <p><strong>{HE(provNombre)}</strong></p>");
            if (!string.IsNullOrEmpty(provDir))  html.AppendLine($"  <p>{HE(provDir)}</p>");
            var provMeta = new List<string>();
            if (!string.IsNullOrEmpty(provTel))  provMeta.Add("Tel: " + HE(provTel));
            if (!string.IsNullOrEmpty(provCuit)) provMeta.Add("CUIT: " + HE(provCuit));
            if (provMeta.Any()) html.AppendLine($"  <p>{string.Join(" &nbsp;|&nbsp; ", provMeta)}</p>");
            html.AppendLine("</div>");

            // Tabla
            html.AppendLine("<table>");
            html.AppendLine("<thead><tr>");
            html.AppendLine("  <th>Cód. Barras</th><th>Cód. Proveedor</th><th>Descripción</th>");
            html.AppendLine("  <th class=\"r\">Precio Proveedor</th><th class=\"r\">Cantidad</th><th class=\"r\">Subtotal</th>");
            html.AppendLine("</tr></thead>");
            html.AppendLine("<tbody>");
            html.Append(sbRows);
            html.AppendLine("</tbody>");
            html.AppendLine("<tfoot><tr class=\"total-row\">");
            html.AppendLine($"  <td colspan=\"5\">TOTAL</td><td class=\"r\">{totalStr}</td>");
            html.AppendLine("</tr></tfoot>");
            html.AppendLine("</table>");

            html.AppendLine($"<div class=\"doc-foot\">Generado el {fechaGen} &nbsp;&mdash;&nbsp; {HE(empNombre)}</div>");
            html.AppendLine("</body></html>");

            var htmlStr = html.ToString();

            if (esExcel)
            {
                var bytes = Encoding.UTF8.GetBytes(htmlStr);
                return File(bytes, "application/vnd.ms-excel",
                    $"OrdenCompra_{nroPedido}_{DateTime.Now:ddMMyyyyHHmm}.xls");
            }

            return Content(htmlStr, "text/html", Encoding.UTF8);
        }

        // ----------------------------------------------------------------
        // REPORTE STOCK / PRODUCTOS A PEDIR (reemplaza ReportViewerCore)
        // ----------------------------------------------------------------
        public async Task<IActionResult> OnGetReporteStockAsync(
            string formato, DateTime desde, DateTime hasta, int proveedorId)
        {
            var productos = await _ordenCompraService.TraerListaProdAPedirImprimirAsync(proveedorId, desde, hasta);
            var cantDec   = await _parametroService.ObtenerCantidadDecimalesProductosAsync();
            var cantStock = await _parametroService.ObtenerCantidadDecimalesStockAsync();

            var empNombre = await _parametroService.ObtenerValorAsync("empresa", "nombre") ?? "";
            var fmtDec    = "N" + cantDec;
            var fmtQ      = "N" + cantStock;

            var prov = proveedorId > 0 ? await _proveedorService.GetByIdAsync(proveedorId) : null;
            var provNombre = prov?.NombreComercial ?? $"Proveedor {proveedorId}";

            var sbRows = new StringBuilder();
            foreach (var p in productos)
            {
                sbRows.Append("<tr>");
                sbRows.Append($"<td>{HE(p.Cod_Prov)}</td>");
                sbRows.Append($"<td>{HE(p.Descripcion)}</td>");
                sbRows.Append($"<td class=\"r\">{p.Stock.ToString(fmtQ, CultureInfo.CurrentCulture)}</td>");
                sbRows.Append($"<td class=\"r\">{p.C_Min.ToString(fmtQ, CultureInfo.CurrentCulture)}</td>");
                sbRows.Append($"<td class=\"r\">{p.Ingreso.ToString(fmtQ, CultureInfo.CurrentCulture)}</td>");
                sbRows.Append($"<td class=\"r\">{p.Ventas.ToString(fmtQ, CultureInfo.CurrentCulture)}</td>");
                sbRows.Append($"<td class=\"r\">{p.Cant.ToString(fmtQ, CultureInfo.CurrentCulture)}</td>");
                sbRows.Append("</tr>");
            }

            var subtitulo = $"Movimientos desde {desde:dd/MM/yyyy} hasta {hasta:dd/MM/yyyy}";
            var fechaGen  = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            var esExcel   = formato.ToLower() == "excel";

            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"UTF-8\">");
            html.AppendLine($"<title>Productos a Pedir — {HE(provNombre)}</title>");
            html.AppendLine("<style>");
            html.AppendLine("*{box-sizing:border-box;margin:0;padding:0;}");
            html.AppendLine("body{font-family:Arial,Helvetica,sans-serif;font-size:9pt;color:#222;padding:10mm;}");
            html.AppendLine(".no-print{padding:6px;background:#f0f4ff;display:flex;gap:8px;margin-bottom:8mm;}");
            html.AppendLine(".no-print button{background:#4e73df;color:#fff;border:none;border-radius:6px;padding:5px 16px;font-size:9pt;cursor:pointer;}");
            html.AppendLine(".no-print .btn-sec{background:#858796;}");
            html.AppendLine(".rpt-header{border-bottom:2px solid #1a2a4a;padding-bottom:4mm;margin-bottom:4mm;}");
            html.AppendLine(".rpt-header h2{font-size:12pt;color:#1a2a4a;margin-bottom:2px;}");
            html.AppendLine(".rpt-header p{font-size:8.5pt;color:#555;}");
            html.AppendLine("table{width:100%;border-collapse:collapse;margin-bottom:3mm;}");
            html.AppendLine("thead tr{background:#1a2a4a;}");
            html.AppendLine("th{padding:2.5mm 3mm;color:#fff;font-size:7.5pt;text-transform:uppercase;letter-spacing:.3px;text-align:left;}");
            html.AppendLine("th.r,td.r{text-align:right;}");
            html.AppendLine("td{padding:2mm 3mm;font-size:8.5pt;border-bottom:1px solid #eee;}");
            html.AppendLine("tbody tr:nth-child(even){background:#f5f8ff;}");
            html.AppendLine(".doc-foot{margin-top:6mm;font-size:7.5pt;color:#aaa;text-align:center;border-top:1px solid #eee;padding-top:2mm;}");
            html.AppendLine("@page{size:A4;margin:8mm;}");
            html.AppendLine("@media print{body{padding:0;}.no-print{display:none!important;}}");
            html.AppendLine("</style></head><body>");

            if (!esExcel)
            {
                html.AppendLine("<div class=\"no-print\">");
                html.AppendLine("  <button onclick=\"window.print()\">🖨 Imprimir / Guardar PDF</button>");
                html.AppendLine("  <button class=\"btn-sec\" onclick=\"window.close()\">✖ Cerrar</button>");
                html.AppendLine("</div>");
            }

            html.AppendLine("<div class=\"rpt-header\">");
            html.AppendLine($"  <h2>Productos a Pedir — {HE(provNombre)}</h2>");
            html.AppendLine($"  <p>{HE(subtitulo)}</p>");
            html.AppendLine("</div>");

            html.AppendLine("<table>");
            html.AppendLine("<thead><tr>");
            html.AppendLine("  <th>Cód. Proveedor</th><th>Descripción</th>");
            html.AppendLine("  <th class=\"r\">Stock</th><th class=\"r\">C. Mín.</th>");
            html.AppendLine("  <th class=\"r\">Ingresos</th><th class=\"r\">Ventas</th><th class=\"r\">A Pedir</th>");
            html.AppendLine("</tr></thead>");
            html.AppendLine("<tbody>");
            html.Append(sbRows);
            html.AppendLine("</tbody></table>");
            html.AppendLine($"<div class=\"doc-foot\">Generado el {fechaGen} &nbsp;&mdash;&nbsp; {HE(empNombre)}</div>");
            html.AppendLine("</body></html>");

            var htmlStr = html.ToString();
            if (esExcel)
            {
                var bytes = Encoding.UTF8.GetBytes(htmlStr);
                return File(bytes, "application/vnd.ms-excel",
                    $"ProductosAPedir_{DateTime.Now:ddMMyyyyHHmm}.xls");
            }
            return Content(htmlStr, "text/html", Encoding.UTF8);
        }

        // ----------------------------------------------------------------
        private static string HE(string? s) =>
            System.Net.WebUtility.HtmlEncode(s ?? "");
    }
}
