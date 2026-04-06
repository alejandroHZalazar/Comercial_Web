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
using System.Text.Json;

namespace Comercial_Web.Pages.Productos.GestionStock;

[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly IGestionStockService _service;
    private readonly IProveedorService    _proveedorService;
    private readonly IRubroService        _rubroService;
    private readonly IParametroService    _parametroService;

    public List<SelectListItem> Proveedores { get; private set; } = new();
    public List<SelectListItem> Rubros      { get; private set; } = new();

    public IndexModel(
        IGestionStockService service,
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
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    // ---------------------------------------------------------------
    // POST AjustarStock → JSON (procesado fila a fila desde el cliente)
    // ---------------------------------------------------------------
    public async Task<JsonResult> OnPostAjustarStockAsync(
        [FromBody] AjusteStockRequest req)
    {
        var result = await _service.AjustarStockAsync(req.ProductoId, req.NuevoStock);
        return new JsonResult(result,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    // ---------------------------------------------------------------
    // POST ExportarExcel → descarga .xls con encabezado de empresa
    // ---------------------------------------------------------------
    public async Task<IActionResult> OnPostExportarExcelAsync(
        [FromBody] List<GestionStockItemDto> filas)
    {
        var nombre      = await _parametroService.ObtenerValorAsync("empresa", "nombre");
        var razonSocial = await _parametroService.ObtenerValorAsync("empresa", "razonSocial");
        var direccion   = await _parametroService.ObtenerValorAsync("empresa", "direccion");
        var localidad   = await _parametroService.ObtenerValorAsync("empresa", "localidad");
        var cuit        = await _parametroService.ObtenerValorAsync("empresa", "cuit");
        var telefono    = await _parametroService.ObtenerValorAsync("empresa", "telefono");
        var logoParam   = await _parametroService.GetLogoAsync();

        var bytes = GenerarExcel(
            filas, logoParam?.Imagen,
            nombre, razonSocial, direccion, localidad, cuit, telefono);

        var fname = $"gestion_stock_{DateTime.Now:yyyyMMdd_HHmmss}.xls";
        return File(bytes, "application/vnd.ms-excel", fname);
    }

    // ---------------------------------------------------------------
    // Generador Excel con NPOI (formato .xls — HSSFWorkbook)
    // ---------------------------------------------------------------
    private static byte[] GenerarExcel(
        List<GestionStockItemDto> filas,
        byte[]? logoBytes,
        string nombre, string razonSocial,
        string direccion, string localidad,
        string cuit, string telefono)
    {
        var wb = new HSSFWorkbook();

        // Paleta de colores customizada
        var palette = wb.GetCustomPalette();
        palette.SetColorAtIndex(40, (byte)26,  (byte)42,  (byte)74);   // #1a2a4a navy
        palette.SetColorAtIndex(41, (byte)244, (byte)246, (byte)251);  // #f4f6fb alt-row
        palette.SetColorAtIndex(42, (byte)238, (byte)242, (byte)255);  // #eef2ff title-bg

        var sheet = wb.CreateSheet("Gestión de Stock");

        // Anchos de columna (unidades: 1/256 de carácter)
        sheet.SetColumnWidth(0, 18 * 256); // Cód. Proveedor
        sheet.SetColumnWidth(1, 50 * 256); // Descripción
        sheet.SetColumnWidth(2, 14 * 256); // Stock
        sheet.SetColumnWidth(3, 14 * 256); // Cant. Mínima
        sheet.SetColumnWidth(4, 16 * 256); // P. Costo
        sheet.SetColumnWidth(5, 16 * 256); // P. Proveedor
        sheet.SetColumnWidth(6, 16 * 256); // P. Lista

        // ---- Fuentes ----
        IFont MakeFont(short pts, bool bold, short colorIdx)
        {
            var f = wb.CreateFont();
            f.FontHeightInPoints = pts;
            f.IsBold = bold;
            f.Color  = colorIdx;
            return f;
        }
        var fNombre   = MakeFont(14, true,  40);
        var fSub      = MakeFont(10, false, IndexedColors.Grey50Percent.Index);
        var fDet      = MakeFont(9,  false, IndexedColors.Grey50Percent.Index);
        var fTitle    = MakeFont(12, true,  40);
        var fHeader   = MakeFont(9,  true,  IndexedColors.White.Index);
        var fData     = MakeFont(9,  false, IndexedColors.Black.Index);
        var fDataBold = MakeFont(9,  true,  IndexedColors.Black.Index);

        // ---- Estilos ----
        ICellStyle MakeStyle(IFont font, short bgColor = -1,
            HorizontalAlignment align = HorizontalAlignment.Left,
            string? numFmt = null, bool borders = false)
        {
            var s = wb.CreateCellStyle();
            s.SetFont(font);
            s.Alignment          = align;
            s.VerticalAlignment  = VerticalAlignment.Center;
            if (bgColor >= 0) { s.FillForegroundColor = bgColor; s.FillPattern = FillPattern.SolidForeground; }
            if (numFmt != null) s.DataFormat = wb.CreateDataFormat().GetFormat(numFmt);
            if (borders)
            {
                s.BorderBottom = BorderStyle.Hair;
                s.BottomBorderColor = IndexedColors.Grey25Percent.Index;
            }
            return s;
        }

        var sNombre    = MakeStyle(fNombre);
        var sSub       = MakeStyle(fSub);
        var sDet       = MakeStyle(fDet);
        var sTitle     = MakeStyle(fTitle, 42, HorizontalAlignment.Center);
        var sTitleDate = MakeStyle(fDet,   42, HorizontalAlignment.Right);
        var sHeader    = MakeStyle(fHeader, 40, HorizontalAlignment.Center);
        sHeader.BorderBottom = BorderStyle.Thin;
        sHeader.BorderTop    = BorderStyle.Thin;
        sHeader.BorderLeft   = BorderStyle.Thin;
        sHeader.BorderRight  = BorderStyle.Thin;

        var sData    = MakeStyle(fData,     -1,  HorizontalAlignment.Left,  null,    true);
        var sDataAlt = MakeStyle(fData,      41, HorizontalAlignment.Left,  null,    true);
        var sNum     = MakeStyle(fData,     -1,  HorizontalAlignment.Right, "#,##0.00##", true);
        var sNumAlt  = MakeStyle(fData,      41, HorizontalAlignment.Right, "#,##0.00##", true);
        var sNumB    = MakeStyle(fDataBold, -1,  HorizontalAlignment.Right, "#,##0.00##", true);
        var sNumBAlt = MakeStyle(fDataBold,  41, HorizontalAlignment.Right, "#,##0.00##", true);

        // ---- Filas de encabezado (0-3) ----
        for (int r = 0; r < 4; r++)
        {
            var row = sheet.CreateRow(r);
            row.HeightInPoints = r == 0 ? 45f : 20f;
        }

        // Logo (cols 0-1, rows 0-3)
        if (logoBytes != null && logoBytes.Length > 0)
        {
            try
            {
                int picIdx  = wb.AddPicture(logoBytes, PictureType.JPEG);
                var drawing = sheet.CreateDrawingPatriarch();
                var anchor  = new HSSFClientAnchor(0, 0, 0, 0, 0, 0, 2, 4);
                anchor.AnchorType = AnchorType.MoveAndResize;
                drawing.CreatePicture(anchor, picIdx);
            }
            catch { /* si el logo falla, continuamos sin él */ }
        }

        // Info empresa (cols 2-6, rows 0-3)
        void SetCell(int ri, int ci, string val, ICellStyle st)
        {
            var row  = sheet.GetRow(ri) ?? sheet.CreateRow(ri);
            var cell = row.CreateCell(ci);
            cell.SetCellValue(val);
            cell.CellStyle = st;
        }

        if (!string.IsNullOrEmpty(nombre))
        {
            SetCell(0, 2, nombre, sNombre);
            sheet.AddMergedRegion(new CellRangeAddress(0, 0, 2, 6));
        }
        if (!string.IsNullOrEmpty(razonSocial))
        {
            SetCell(1, 2, razonSocial, sSub);
            sheet.AddMergedRegion(new CellRangeAddress(1, 1, 2, 6));
        }
        var dirCiudad = string.Join(" | ",
            new[] { direccion, localidad }.Where(s => !string.IsNullOrEmpty(s)));
        if (!string.IsNullOrEmpty(dirCiudad))
        {
            SetCell(2, 2, dirCiudad, sDet);
            sheet.AddMergedRegion(new CellRangeAddress(2, 2, 2, 6));
        }
        var fiscal = string.Join("   ",
            new[] {
                !string.IsNullOrEmpty(cuit)     ? $"CUIT: {cuit}"     : null,
                !string.IsNullOrEmpty(telefono) ? $"Tel: {telefono}"  : null
            }.Where(s => s != null));
        if (!string.IsNullOrEmpty(fiscal))
        {
            SetCell(3, 2, fiscal, sDet);
            sheet.AddMergedRegion(new CellRangeAddress(3, 3, 2, 6));
        }

        // ---- Fila título ----
        int rowIdx = 4;
        var titleRow = sheet.CreateRow(rowIdx);
        titleRow.HeightInPoints = 26f;
        var tc = titleRow.CreateCell(0);
        tc.SetCellValue("GESTIÓN DE STOCK");
        tc.CellStyle = sTitle;
        sheet.AddMergedRegion(new CellRangeAddress(rowIdx, rowIdx, 0, 5));
        var dc = titleRow.CreateCell(6);
        dc.SetCellValue($"{DateTime.Now:dd/MM/yyyy HH:mm}");
        dc.CellStyle = sTitleDate;
        rowIdx++;

        // ---- Encabezados de columna ----
        var hRow = sheet.CreateRow(rowIdx);
        hRow.HeightInPoints = 20f;
        string[] cols = {
            "Cód. Proveedor", "Descripción",
            "Stock", "Cant. Mínima",
            "P. Costo", "P. Proveedor", "P. Lista"
        };
        for (int ci = 0; ci < cols.Length; ci++)
        {
            var cell = hRow.CreateCell(ci);
            cell.SetCellValue(cols[ci]);
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

            ICell C(int ci, string v, ICellStyle st)
            { var c = dr.CreateCell(ci); c.SetCellValue(v); c.CellStyle = st; return c; }
            ICell N(int ci, double v, ICellStyle st)
            { var c = dr.CreateCell(ci); c.SetCellValue(v); c.CellStyle = st; return c; }

            var sd = alt ? sDataAlt : sData;
            var sn = alt ? sNumAlt  : sNum;
            var sb = alt ? sNumBAlt : sNumB;

            C(0, f.CodProveedor, sd);
            C(1, f.Descripcion,  sd);
            N(2, (double)f.Stock,           sn);
            N(3, (double)f.CantidadMinima,  sn);
            N(4, (double)f.PrecioCosto,     sn);
            N(5, (double)f.PrecioProveedor, sn);
            N(6, (double)f.PrecioLista,     sb);
        }

        // ---- Fila total al pie ----
        rowIdx += filas.Count;
        var totalRow = sheet.CreateRow(rowIdx);
        totalRow.HeightInPoints = 16f;
        var totStyle = MakeStyle(fDet, -1, HorizontalAlignment.Right);
        var totCell  = totalRow.CreateCell(0);
        totCell.SetCellValue($"Total: {filas.Count} producto(s) — {DateTime.Now:dd/MM/yyyy HH:mm}");
        totCell.CellStyle = totStyle;
        sheet.AddMergedRegion(new CellRangeAddress(rowIdx, rowIdx, 0, 6));

        using var ms = new MemoryStream();
        wb.Write(ms);
        return ms.ToArray();
    }
}
