using Application.Interfaces;
using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using System.Text;
using System.Text.Json;

namespace Comercial_Web.Pages.Productos.ListaPrecios;

[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly IListaPreciosService _service;
    private readonly IProveedorService    _proveedorService;
    private readonly IRubroService        _rubroService;
    private readonly IParametroService    _parametroService;

    public List<SelectListItem> Proveedores { get; private set; } = new();
    public List<SelectListItem> Rubros      { get; private set; } = new();

    public IndexModel(
        IListaPreciosService service,
        IProveedorService    proveedorService,
        IRubroService        rubroService,
        IParametroService    parametroService)
    {
        _service          = service;
        _proveedorService = proveedorService;
        _rubroService     = rubroService;
        _parametroService = parametroService;
    }

    // ---------------------------------------------------------------
    // GET — carga inicial
    // ---------------------------------------------------------------
    public async Task OnGetAsync()
    {
        var provs = await _proveedorService.GetAllAsync();
        Proveedores = provs
            .Where(p => p.Baja != true)
            .OrderBy(p => p.NombreComercial)
            .Select(p => new SelectListItem(
                p.NombreComercial ?? p.Id.ToString(),
                p.Id.ToString()))
            .ToList();

        var rubs = await _rubroService.GetAllAsync();
        Rubros = rubs
            .OrderBy(r => r.Descripcion)
            .Select(r => new SelectListItem(
                r.Descripcion ?? r.Id.ToString(),
                r.Id.ToString()))
            .ToList();
    }

    // ---------------------------------------------------------------
    // GET Buscar → JSON
    // ---------------------------------------------------------------
    public async Task<JsonResult> OnGetBuscarAsync(
        [FromQuery] int[]   proveedores,
        [FromQuery] int[]   rubros,
        [FromQuery] string? texto)
    {
        var items = await _service.BuscarAsync(
            proveedores.Length > 0 ? proveedores : null,
            rubros.Length      > 0 ? rubros      : null,
            texto);

        return new JsonResult(items,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
    }

    // ---------------------------------------------------------------
    // POST ExportarCsv → descarga de archivo
    // ---------------------------------------------------------------
    public async Task<IActionResult> OnPostExportarCsvAsync([FromBody] List<ListaPrecioItemDto> filas)
    {
        // Datos de la empresa (mismos que el PDF)
        var nombre      = await _parametroService.ObtenerValorAsync("empresa", "nombre");
        var razonSocial = await _parametroService.ObtenerValorAsync("empresa", "razonSocial");
        var direccion   = await _parametroService.ObtenerValorAsync("empresa", "direccion");
        var localidad   = await _parametroService.ObtenerValorAsync("empresa", "localidad");
        var cuit        = await _parametroService.ObtenerValorAsync("empresa", "cuit");
        var telefono    = await _parametroService.ObtenerValorAsync("empresa", "telefono");

        var sb = new StringBuilder();

        // ---- Encabezado empresa ----
        if (!string.IsNullOrEmpty(nombre))
            sb.AppendLine(nombre);
        if (!string.IsNullOrEmpty(razonSocial))
            sb.AppendLine(razonSocial);

        var detalle = new List<string>();
        if (!string.IsNullOrEmpty(direccion)) detalle.Add(direccion);
        if (!string.IsNullOrEmpty(localidad)) detalle.Add(localidad);
        if (detalle.Count > 0)
            sb.AppendLine(string.Join(" | ", detalle));

        var fiscal = new List<string>();
        if (!string.IsNullOrEmpty(cuit))     fiscal.Add($"CUIT: {cuit}");
        if (!string.IsNullOrEmpty(telefono)) fiscal.Add($"Tel: {telefono}");
        if (fiscal.Count > 0)
            sb.AppendLine(string.Join(" | ", fiscal));

        sb.AppendLine($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Total de productos: {filas.Count}");
        sb.AppendLine(); // línea en blanco separadora

        // ---- Encabezado de columnas ----
        sb.AppendLine("Cod. Proveedor;Cod. Barras;Descripcion;Precio sin IVA;Precio con IVA (21%)");

        // ---- Datos ----
        foreach (var f in filas)
        {
            var desc = (f.Descripcion ?? "").Replace(";", ",").Replace("\"", "\"\"");
            sb.AppendLine(
                $"\"{f.CodProveedor}\";" +
                $"\"{f.CodBarras}\";" +
                $"\"{desc}\";" +
                $"{f.PrecioSinIva.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)};" +
                $"{f.PrecioConIva.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}");
        }

        // UTF-8 con BOM para compatibilidad con Excel
        var bom   = Encoding.UTF8.GetPreamble();
        var body  = Encoding.UTF8.GetBytes(sb.ToString());
        var bytes = bom.Concat(body).ToArray();
        var fname = $"lista_precios_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        return File(bytes, "text/csv;charset=utf-8", fname);
    }

    // ---------------------------------------------------------------
    // POST ExportarHtml → página imprimible (PDF via browser print)
    // ---------------------------------------------------------------
    public async Task<ContentResult> OnPostExportarHtmlAsync(
        [FromBody] List<ListaPrecioItemDto> filas)
    {
        var nombre      = await _parametroService.ObtenerValorAsync("empresa", "nombre");
        var razonSocial = await _parametroService.ObtenerValorAsync("empresa", "razonSocial");
        var direccion   = await _parametroService.ObtenerValorAsync("empresa", "direccion");
        var localidad   = await _parametroService.ObtenerValorAsync("empresa", "localidad");
        var cuit        = await _parametroService.ObtenerValorAsync("empresa", "cuit");
        var telefono    = await _parametroService.ObtenerValorAsync("empresa", "telefono");

        var logoParam = await _parametroService.GetLogoAsync();
        var logoHtml  = string.Empty;
        if (logoParam?.Imagen != null && logoParam.Imagen.Length > 0)
        {
            var b64 = Convert.ToBase64String(logoParam.Imagen);
            logoHtml = $"<img src=\"data:image/jpeg;base64,{b64}\" " +
                       "style=\"max-height:90px;max-width:220px;object-fit:contain;\" />";
        }

        var html = GenerarHtmlReporte(
            filas, logoHtml,
            nombre, razonSocial,
            direccion, localidad,
            cuit, telefono);

        return Content(html, "text/html; charset=utf-8");
    }

    // ---------------------------------------------------------------
    // Generador de HTML imprimible
    // ---------------------------------------------------------------
    private static string GenerarHtmlReporte(
        List<ListaPrecioItemDto> filas,
        string logoHtml,
        string nombre,      string razonSocial,
        string direccion,   string localidad,
        string cuit,        string telefono)
    {
        static string H(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");
        static string F(decimal d) => d.ToString("N2", new System.Globalization.CultureInfo("es-AR"));

        var sb = new StringBuilder();

        sb.Append("""
            <!DOCTYPE html>
            <html lang="es">
            <head>
            <meta charset="UTF-8">
            <title>Lista de Precios</title>
            <style>
              * { box-sizing: border-box; margin: 0; padding: 0; }
              body { font-family: Arial, Helvetica, sans-serif; font-size: 10.5pt; color: #111; background: #fff; padding: 20px; }
              .encabezado { display: flex; align-items: flex-start; justify-content: space-between; gap: 18px; border-bottom: 2.5px solid #1a2a4a; padding-bottom: 12px; margin-bottom: 14px; }
              .enc-logo { flex-shrink: 0; }
              .enc-datos { flex: 1; }
              .enc-nombre { font-size: 14pt; font-weight: 700; color: #1a2a4a; line-height: 1.2; }
              .enc-razon  { font-size: 9.5pt; color: #444; margin-top: 2px; }
              .enc-detalle { font-size: 8.5pt; color: #666; margin-top: 5px; line-height: 1.6; }
              .enc-fecha  { font-size: 8.5pt; color: #777; text-align: right; white-space: nowrap; }
              .titulo { font-size: 12pt; font-weight: 700; color: #1a2a4a; text-align: center; margin-bottom: 10px; letter-spacing: .5px; text-transform: uppercase; }
              table { width: 100%; border-collapse: collapse; }
              thead tr { background: #1a2a4a; }
              th { padding: 6px 8px; color: #fff; font-size: 8pt; font-weight: 700; text-transform: uppercase; letter-spacing: .4px; white-space: nowrap; text-align: left; }
              th.r, td.r { text-align: right; }
              tbody tr:nth-child(even) { background: #f2f5fb; }
              td { padding: 4px 8px; font-size: 9.5pt; border-bottom: 1px solid #dde3ee; vertical-align: middle; }
              .pie { margin-top: 14px; font-size: 8pt; color: #aaa; text-align: center; border-top: 1px solid #ddd; padding-top: 6px; }
              @media print {
                body { -webkit-print-color-adjust: exact; print-color-adjust: exact; padding: 10px; }
                thead tr { background: #1a2a4a !important; color: #fff !important; }
                .no-print { display: none !important; }
              }
            </style>
            </head>
            <body>
            """);

        // Encabezado empresa
        sb.Append("<div class=\"encabezado\">");
        sb.Append("<div class=\"enc-logo\">").Append(logoHtml).Append("</div>");
        sb.Append("<div class=\"enc-datos\">");
        if (!string.IsNullOrEmpty(nombre))
            sb.Append($"<div class=\"enc-nombre\">{H(nombre)}</div>");
        if (!string.IsNullOrEmpty(razonSocial))
            sb.Append($"<div class=\"enc-razon\">{H(razonSocial)}</div>");
        var det = new List<string>();
        if (!string.IsNullOrEmpty(direccion)) det.Add(H(direccion));
        if (!string.IsNullOrEmpty(localidad)) det.Add(H(localidad));
        if (!string.IsNullOrEmpty(cuit))      det.Add("CUIT: " + H(cuit));
        if (!string.IsNullOrEmpty(telefono))  det.Add("Tel: "  + H(telefono));
        if (det.Count > 0)
            sb.Append($"<div class=\"enc-detalle\">{string.Join(" &nbsp;|&nbsp; ", det)}</div>");
        sb.Append("</div>");
        sb.Append($"<div class=\"enc-fecha\">Fecha: {DateTime.Now:dd/MM/yyyy}<br/><strong>{DateTime.Now:HH:mm}</strong></div>");
        sb.Append("</div>");

        // Título
        sb.Append("<div class=\"titulo\">Lista de Precios</div>");

        // Tabla
        sb.Append("""
            <table>
            <thead><tr>
              <th>Cód. Proveedor</th>
              <th>Cód. de Barras</th>
              <th>Descripción</th>
              <th class="r">Precio sin IVA</th>
              <th class="r">Precio con IVA (21%)</th>
            </tr></thead>
            <tbody>
            """);

        foreach (var f in filas)
        {
            sb.Append("<tr>");
            sb.Append($"<td>{H(f.CodProveedor)}</td>");
            sb.Append($"<td>{H(f.CodBarras)}</td>");
            sb.Append($"<td>{H(f.Descripcion)}</td>");
            sb.Append($"<td class=\"r\">{F(f.PrecioSinIva)}</td>");
            sb.Append($"<td class=\"r\">{F(f.PrecioConIva)}</td>");
            sb.Append("</tr>");
        }

        sb.Append("</tbody></table>");
        sb.Append($"<div class=\"pie\">");
        if (!string.IsNullOrEmpty(nombre))
            sb.Append($"{H(nombre)} &mdash; ");
        sb.Append($"Generado el {DateTime.Now:dd/MM/yyyy} a las {DateTime.Now:HH:mm} &mdash; {filas.Count} producto(s)</div>");

        sb.Append("""
            <script>window.onload = function () { window.print(); };</script>
            </body></html>
            """);

        return sb.ToString();
    }

    // ---------------------------------------------------------------
    // POST ExportarExcel → descarga .xls con encabezado empresa + logo
    // ---------------------------------------------------------------
    public async Task<IActionResult> OnPostExportarExcelAsync([FromBody] List<ListaPrecioItemDto> filas)
    {
        var nombre      = await _parametroService.ObtenerValorAsync("empresa", "nombre");
        var razonSocial = await _parametroService.ObtenerValorAsync("empresa", "razonSocial");
        var direccion   = await _parametroService.ObtenerValorAsync("empresa", "direccion");
        var localidad   = await _parametroService.ObtenerValorAsync("empresa", "localidad");
        var cuit        = await _parametroService.ObtenerValorAsync("empresa", "cuit");
        var telefono    = await _parametroService.ObtenerValorAsync("empresa", "telefono");
        var logoParam   = await _parametroService.GetLogoAsync();

        var wb = new HSSFWorkbook();

        // Paleta personalizada: sobreescribimos índices seguros (40-42)
        var palette = wb.GetCustomPalette();
        palette.SetColorAtIndex(40, (byte)26,  (byte)42,  (byte)74);    // #1a2a4a  azul marino
        palette.SetColorAtIndex(41, (byte)244, (byte)246, (byte)251);   // #f4f6fb  fila alternada
        palette.SetColorAtIndex(42, (byte)238, (byte)242, (byte)255);   // #eef2ff  fondo título

        var sheet = wb.CreateSheet("Lista de Precios");
        sheet.SetColumnWidth(0, 18 * 256);   // Cód Proveedor
        sheet.SetColumnWidth(1, 20 * 256);   // Cód Barras
        sheet.SetColumnWidth(2, 50 * 256);   // Descripción
        sheet.SetColumnWidth(3, 18 * 256);   // Precio sin IVA
        sheet.SetColumnWidth(4, 18 * 256);   // Precio con IVA

        // ---- Fuentes ----
        var fNombre   = CrearFuente(wb, 14, bold: true,  color: 40);
        var fSubtitle = CrearFuente(wb, 10, bold: false, color: IndexedColors.Grey50Percent.Index);
        var fDetail   = CrearFuente(wb,  9, bold: false, color: IndexedColors.Grey50Percent.Index);
        var fTitulo   = CrearFuente(wb, 13, bold: true,  color: 40);
        var fHeader   = CrearFuente(wb,  9, bold: true,  color: IndexedColors.White.Index);
        var fData     = CrearFuente(wb,  9, bold: false, color: IndexedColors.Black.Index);
        var fDataBold = CrearFuente(wb,  9, bold: true,  color: IndexedColors.Black.Index);

        // ---- Formato numérico ----
        short numFmt = wb.CreateDataFormat().GetFormat("#,##0.00");

        // ---- Estilos ----
        var sNombre   = wb.CreateCellStyle(); sNombre.SetFont(fNombre);   sNombre.VerticalAlignment = VerticalAlignment.Center;
        var sSubtitle = wb.CreateCellStyle(); sSubtitle.SetFont(fSubtitle); sSubtitle.VerticalAlignment = VerticalAlignment.Center;
        var sDetail   = wb.CreateCellStyle(); sDetail.SetFont(fDetail);   sDetail.VerticalAlignment = VerticalAlignment.Center;

        var sTitulo = wb.CreateCellStyle();
        sTitulo.SetFont(fTitulo);
        sTitulo.FillForegroundColor = 42; sTitulo.FillPattern = FillPattern.SolidForeground;
        sTitulo.Alignment = HorizontalAlignment.Center; sTitulo.VerticalAlignment = VerticalAlignment.Center;
        sTitulo.BorderBottom = BorderStyle.Medium; sTitulo.BottomBorderColor = 40;

        var sTituloFecha = wb.CreateCellStyle();
        sTituloFecha.SetFont(fDetail);
        sTituloFecha.FillForegroundColor = 42; sTituloFecha.FillPattern = FillPattern.SolidForeground;
        sTituloFecha.Alignment = HorizontalAlignment.Right; sTituloFecha.VerticalAlignment = VerticalAlignment.Center;

        var sHeader = wb.CreateCellStyle();
        sHeader.SetFont(fHeader);
        sHeader.FillForegroundColor = 40; sHeader.FillPattern = FillPattern.SolidForeground;
        sHeader.Alignment = HorizontalAlignment.Center; sHeader.VerticalAlignment = VerticalAlignment.Center;
        sHeader.BorderBottom = BorderStyle.Thin; sHeader.BorderTop = BorderStyle.Thin;
        sHeader.BorderLeft  = BorderStyle.Thin; sHeader.BorderRight = BorderStyle.Thin;

        var sData    = EstiloTexto(wb, fData,     altRow: false, numFmt: 0);
        var sDataAlt = EstiloTexto(wb, fData,     altRow: true,  numFmt: 0);
        var sNum     = EstiloTexto(wb, fData,     altRow: false, numFmt: numFmt, right: true);
        var sNumAlt  = EstiloTexto(wb, fData,     altRow: true,  numFmt: numFmt, right: true);
        var sNumB    = EstiloTexto(wb, fDataBold, altRow: false, numFmt: numFmt, right: true);
        var sNumBAlt = EstiloTexto(wb, fDataBold, altRow: true,  numFmt: numFmt, right: true);

        // ---- Filas 0-3: área del encabezado (logo + datos empresa) ----
        for (int i = 0; i < 4; i++)
        {
            var r = sheet.CreateRow(i);
            r.HeightInPoints = i == 0 ? 42f : 20f;
        }

        // Logo
        if (logoParam?.Imagen != null && logoParam.Imagen.Length > 0)
        {
            int picIdx = wb.AddPicture(logoParam.Imagen, PictureType.JPEG);
            var drawing = sheet.CreateDrawingPatriarch();
            // Ancla: (dx1,dy1,dx2,dy2, col1,row1, col2,row2)
            var anchor = new HSSFClientAnchor(0, 0, 0, 0, 0, 0, 2, 4);
            anchor.AnchorType = AnchorType.MoveAndResize;
            drawing.CreatePicture(anchor, picIdx);
        }

        // Datos empresa en columnas 2-4
        void SetVal(int ri, int ci, string val, ICellStyle? sty = null)
        {
            var row  = sheet.GetRow(ri) ?? sheet.CreateRow(ri);
            var cell = row.CreateCell(ci);
            cell.SetCellValue(val);
            if (sty != null) cell.CellStyle = sty;
        }

        if (!string.IsNullOrEmpty(nombre))     { SetVal(0, 2, nombre,      sNombre);   sheet.AddMergedRegion(new CellRangeAddress(0, 0, 2, 4)); }
        if (!string.IsNullOrEmpty(razonSocial)){ SetVal(1, 2, razonSocial,  sSubtitle); sheet.AddMergedRegion(new CellRangeAddress(1, 1, 2, 4)); }

        var dirInfo = string.Join(" | ", new[] { direccion, localidad }.Where(s => !string.IsNullOrEmpty(s)));
        if (!string.IsNullOrEmpty(dirInfo))    { SetVal(2, 2, dirInfo,      sDetail);   sheet.AddMergedRegion(new CellRangeAddress(2, 2, 2, 4)); }

        var fiscalInfo = string.Join("   ",
            new[] { (!string.IsNullOrEmpty(cuit)     ? $"CUIT: {cuit}"     : null),
                    (!string.IsNullOrEmpty(telefono)  ? $"Tel: {telefono}"  : null) }
            .Where(s => s != null));
        if (!string.IsNullOrEmpty(fiscalInfo)) { SetVal(3, 2, fiscalInfo,   sDetail);   sheet.AddMergedRegion(new CellRangeAddress(3, 3, 2, 4)); }

        int rowIdx = 4;

        // ---- Fila de título ----
        var titleRow = sheet.CreateRow(rowIdx); titleRow.HeightInPoints = 26f;
        var tc = titleRow.CreateCell(0); tc.SetCellValue("LISTA DE PRECIOS"); tc.CellStyle = sTitulo;
        sheet.AddMergedRegion(new CellRangeAddress(rowIdx, rowIdx, 0, 3));
        var dc = titleRow.CreateCell(4); dc.SetCellValue($"{DateTime.Now:dd/MM/yyyy HH:mm}"); dc.CellStyle = sTituloFecha;
        rowIdx++;

        // ---- Encabezado de columnas ----
        var hRow = sheet.CreateRow(rowIdx); hRow.HeightInPoints = 22f;
        string[] cols = { "Cód. Proveedor", "Cód. de Barras", "Descripción", "Precio sin IVA", "Precio con IVA (21%)" };
        for (int c = 0; c < cols.Length; c++)
        {
            var cell = hRow.CreateCell(c);
            cell.SetCellValue(cols[c]);
            cell.CellStyle = sHeader;
        }
        rowIdx++;

        // ---- Filas de datos ----
        for (int i = 0; i < filas.Count; i++)
        {
            var f   = filas[i];
            bool alt = i % 2 == 1;
            var dr  = sheet.CreateRow(rowIdx + i);
            dr.HeightInPoints = 15f;

            var c0 = dr.CreateCell(0); c0.SetCellValue(f.CodProveedor); c0.CellStyle = alt ? sDataAlt : sData;
            var c1 = dr.CreateCell(1); c1.SetCellValue(f.CodBarras);    c1.CellStyle = alt ? sDataAlt : sData;
            var c2 = dr.CreateCell(2); c2.SetCellValue(f.Descripcion);  c2.CellStyle = alt ? sDataAlt : sData;
            var c3 = dr.CreateCell(3); c3.SetCellValue((double)f.PrecioSinIva); c3.CellStyle = alt ? sNumAlt : sNum;
            var c4 = dr.CreateCell(4); c4.SetCellValue((double)f.PrecioConIva); c4.CellStyle = alt ? sNumBAlt : sNumB;
        }

        using var ms = new MemoryStream();
        wb.Write(ms);
        var bytes = ms.ToArray();
        var fname = $"lista_precios_{DateTime.Now:yyyyMMdd_HHmmss}.xls";
        return File(bytes, "application/vnd.ms-excel", fname);
    }

    // ---- Helpers NPOI ----
    private static IFont CrearFuente(HSSFWorkbook wb, short size, bool bold, short color)
    {
        var f = wb.CreateFont();
        f.FontHeightInPoints = size;
        f.IsBold  = bold;
        f.Color   = color;
        return f;
    }

    private static ICellStyle EstiloTexto(HSSFWorkbook wb, IFont font,
        bool altRow, short numFmt, bool right = false)
    {
        var s = wb.CreateCellStyle();
        s.SetFont(font);
        if (altRow) { s.FillForegroundColor = 41; s.FillPattern = FillPattern.SolidForeground; }
        if (numFmt  != 0) s.DataFormat = numFmt;
        if (right) s.Alignment = HorizontalAlignment.Right;
        s.BorderBottom     = BorderStyle.Hair;
        s.BottomBorderColor = IndexedColors.Grey25Percent.Index;
        return s;
    }
}
