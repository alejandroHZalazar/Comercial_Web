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

namespace Comercial_Web.Pages.Productos.MovimientosProductos;

[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly IMovimientosProductosService _service;
    private readonly IProveedorService            _proveedorService;
    private readonly IParametroService            _parametroService;

    public List<SelectListItem> Proveedores { get; private set; } = new();

    public IndexModel(
        IMovimientosProductosService service,
        IProveedorService            proveedorService,
        IParametroService            parametroService)
    {
        _service          = service;
        _proveedorService = proveedorService;
        _parametroService = parametroService;
    }

    // ---------------------------------------------------------------
    // GET
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
    }

    // ---------------------------------------------------------------
    // GET: buscar lotes (Tab 1)
    // ---------------------------------------------------------------
    public async Task<JsonResult> OnGetBuscarLotesAsync(
        [FromQuery] int[]   proveedores,
        [FromQuery] string? texto,
        [FromQuery] string? nroComprobante,
        [FromQuery] string? fechaDesde,
        [FromQuery] string? fechaHasta)
    {
        DateTime? desde = TryParseDate(fechaDesde);
        DateTime? hasta = TryParseDate(fechaHasta);

        var lotes = await _service.BuscarLotesAsync(
            proveedores.Length > 0 ? proveedores : null,
            texto, nroComprobante, desde, hasta);

        return Json(lotes);
    }

    // ---------------------------------------------------------------
    // GET: detalle de un lote (Tab 1)
    // ---------------------------------------------------------------
    public async Task<JsonResult> OnGetDetallesLoteAsync(
        [FromQuery] string nroComprobante,
        [FromQuery] string fechaMov)
    {
        if (!DateTime.TryParse(fechaMov, out var fecha))
            return Json(new List<LoteIngresoDetalleDto>());

        var detalles = await _service.BuscarDetallesLoteAsync(nroComprobante, fecha);
        return Json(detalles);
    }

    // ---------------------------------------------------------------
    // GET: buscar productos con movimientos (Tab 2)
    // ---------------------------------------------------------------
    public async Task<JsonResult> OnGetBuscarProductosAsync(
        [FromQuery] string? texto,
        [FromQuery] string? fechaDesde,
        [FromQuery] string? fechaHasta)
    {
        DateTime? desde = TryParseDate(fechaDesde);
        DateTime? hasta = TryParseDate(fechaHasta);

        var productos = await _service.BuscarProductosAsync(texto, desde, hasta);
        return Json(productos);
    }

    // ---------------------------------------------------------------
    // GET: movimientos de un producto (Tab 2)
    // ---------------------------------------------------------------
    public async Task<JsonResult> OnGetMovimientosProductoAsync(
        [FromQuery] int     productoId,
        [FromQuery] string? fechaDesde,
        [FromQuery] string? fechaHasta)
    {
        DateTime? desde = TryParseDate(fechaDesde);
        DateTime? hasta = TryParseDate(fechaHasta);

        var movs = await _service.BuscarMovimientosProductoAsync(productoId, desde, hasta);
        return Json(movs);
    }

    // ---------------------------------------------------------------
    // POST: exportar Excel Tab 1 (detalle lote)
    // ---------------------------------------------------------------
    public async Task<IActionResult> OnPostExportarLoteAsync(
        [FromBody] List<LoteIngresoDetalleDto> filas)
    {
        var (nombre, razonSocial, direccion, localidad, cuit, telefono, logo) =
            await ObtenerDatosEmpresaAsync();

        var bytes = ExcelLote(filas, logo, nombre, razonSocial, direccion, localidad, cuit, telefono);
        return File(bytes, "application/vnd.ms-excel",
            $"lote_ingreso_{DateTime.Now:yyyyMMdd_HHmmss}.xls");
    }

    // ---------------------------------------------------------------
    // POST: exportar Excel Tab 2 (movimientos de un producto)
    // ---------------------------------------------------------------
    public async Task<IActionResult> OnPostExportarMovimientosAsync(
        [FromBody] ExportarMovimientosRequest req)
    {
        var (nombre, razonSocial, direccion, localidad, cuit, telefono, logo) =
            await ObtenerDatosEmpresaAsync();

        var bytes = ExcelMovimientos(
            req.Filas, logo, req.Descripcion,
            nombre, razonSocial, direccion, localidad, cuit, telefono);

        return File(bytes, "application/vnd.ms-excel",
            $"movimientos_{DateTime.Now:yyyyMMdd_HHmmss}.xls");
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------
    private static DateTime? TryParseDate(string? s) =>
        DateTime.TryParse(s, out var d) ? d : null;

    private JsonResult Json(object data) =>
        new JsonResult(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

    private async Task<(string nombre, string razonSocial, string direccion,
        string localidad, string cuit, string telefono, byte[]? logo)>
        ObtenerDatosEmpresaAsync()
    {
        var nombre      = await _parametroService.ObtenerValorAsync("empresa", "nombre");
        var razonSocial = await _parametroService.ObtenerValorAsync("empresa", "razonSocial");
        var direccion   = await _parametroService.ObtenerValorAsync("empresa", "direccion");
        var localidad   = await _parametroService.ObtenerValorAsync("empresa", "localidad");
        var cuit        = await _parametroService.ObtenerValorAsync("empresa", "cuit");
        var telefono    = await _parametroService.ObtenerValorAsync("empresa", "telefono");
        var logoParam   = await _parametroService.GetLogoAsync();
        return (nombre, razonSocial, direccion, localidad, cuit, telefono, logoParam?.Imagen);
    }

    // ================================================================
    // GENERADORES EXCEL (NPOI — .xls)
    // ================================================================
    private static byte[] ExcelLote(
        List<LoteIngresoDetalleDto> filas,
        byte[]? logoBytes,
        string nombre, string razonSocial,
        string direccion, string localidad,
        string cuit, string telefono)
    {
        var wb    = new HSSFWorkbook();
        var sheet = wb.CreateSheet("Lote de Ingreso");

        // Columnas
        int[] anchos = { 18, 45, 14, 20, 12, 12, 14, 18, 18 };
        for (int i = 0; i < anchos.Length; i++)
            sheet.SetColumnWidth(i, anchos[i] * 256);

        var styles = BuildStyles(wb);
        int rowIdx = InsertarEncabezadoEmpresa(wb, sheet, styles, logoBytes,
            nombre, razonSocial, direccion, localidad, cuit, telefono,
            "DETALLE DE LOTE DE INGRESO", 9);

        // Cabecera columnas
        string[] cols = { "Cód. Proveedor", "Descripción", "Fecha Entrega",
                          "Tipo Movimiento", "Stock Ant.", "Stock Act.",
                          "Cantidad", "Costo", "Nro. Comprobante" };
        rowIdx = InsertarCabeceraColumnas(sheet, styles, rowIdx, cols);

        // Datos
        for (int i = 0; i < filas.Count; i++)
        {
            var f   = filas[i];
            bool alt = i % 2 == 1;
            var dr  = sheet.CreateRow(rowIdx++);
            dr.HeightInPoints = 15f;
            var sd = alt ? styles.DataAlt : styles.Data;
            var sn = alt ? styles.NumAlt  : styles.Num;

            SetStr(dr, 0, f.CodProveedor,  sd);
            SetStr(dr, 1, f.Descripcion,   sd);
            SetStr(dr, 2, f.FechaEntrega.HasValue
                ? f.FechaEntrega.Value.ToString("dd/MM/yyyy") : "", sd);
            SetStr(dr, 3, f.Tipo,          sd);
            SetNum(dr, 4, (double)f.StockAnt,  sn);
            SetNum(dr, 5, (double)f.StockAct,  sn);
            SetNum(dr, 6, (double)f.Cantidad,  sn);
            SetNum(dr, 7, (double)f.Costo,     sn);
            SetStr(dr, 8, f.NroComprobante, sd);
        }

        InsertarPie(sheet, styles, rowIdx, filas.Count, 9);

        using var ms = new MemoryStream();
        wb.Write(ms);
        return ms.ToArray();
    }

    private static byte[] ExcelMovimientos(
        List<MovimientoItemDto> filas,
        byte[]? logoBytes,
        string productoDesc,
        string nombre, string razonSocial,
        string direccion, string localidad,
        string cuit, string telefono)
    {
        var wb    = new HSSFWorkbook();
        var sheet = wb.CreateSheet("Movimientos");

        int[] anchos = { 18, 45, 16, 20, 12, 12, 12, 20 };
        for (int i = 0; i < anchos.Length; i++)
            sheet.SetColumnWidth(i, anchos[i] * 256);

        var styles = BuildStyles(wb);

        // Subtítulo con nombre del producto
        int rowIdx = InsertarEncabezadoEmpresa(wb, sheet, styles, logoBytes,
            nombre, razonSocial, direccion, localidad, cuit, telefono,
            "MOVIMIENTOS DE PRODUCTO", 8);

        // Fila con producto
        if (!string.IsNullOrEmpty(productoDesc))
        {
            var pr = sheet.CreateRow(rowIdx++);
            pr.HeightInPoints = 16f;
            var c  = pr.CreateCell(0);
            c.SetCellValue("Producto: " + productoDesc);
            c.CellStyle = styles.Sub;
            sheet.AddMergedRegion(new CellRangeAddress(rowIdx - 1, rowIdx - 1, 0, 7));
        }

        string[] cols = { "Cód. Proveedor", "Descripción", "Fecha Mov.",
                          "Tipo", "Stock Ant.", "Cantidad", "Stock Act.", "Nro. Comprobante" };
        rowIdx = InsertarCabeceraColumnas(sheet, styles, rowIdx, cols);

        for (int i = 0; i < filas.Count; i++)
        {
            var f   = filas[i];
            bool alt = i % 2 == 1;
            var dr  = sheet.CreateRow(rowIdx++);
            dr.HeightInPoints = 15f;
            var sd = alt ? styles.DataAlt : styles.Data;
            var sn = alt ? styles.NumAlt  : styles.Num;

            SetStr(dr, 0, f.CodProveedor,  sd);
            SetStr(dr, 1, f.Descripcion,   sd);
            SetStr(dr, 2, f.FechaMov.HasValue
                ? f.FechaMov.Value.ToString("dd/MM/yyyy HH:mm") : "", sd);
            SetStr(dr, 3, f.Tipo,          sd);
            SetNum(dr, 4, (double)f.StockAnt,  sn);
            SetNum(dr, 5, (double)f.Cantidad,  sn);
            SetNum(dr, 6, (double)f.StockAct,  sn);
            SetStr(dr, 7, f.NroComprobante, sd);
        }

        InsertarPie(sheet, styles, rowIdx, filas.Count, 8);

        using var ms = new MemoryStream();
        wb.Write(ms);
        return ms.ToArray();
    }

    // ----------------------------------------------------------------
    // Helpers NPOI comunes
    // ----------------------------------------------------------------
    private record ExcelStyles(
        ICellStyle Nombre, ICellStyle Sub, ICellStyle Det,
        ICellStyle TitleBg, ICellStyle TitleDate,
        ICellStyle Header,
        ICellStyle Data, ICellStyle DataAlt,
        ICellStyle Num,  ICellStyle NumAlt,
        ICellStyle Pie);

    private static ExcelStyles BuildStyles(HSSFWorkbook wb)
    {
        var palette = wb.GetCustomPalette();
        palette.SetColorAtIndex(40, 26,  42,  74);   // navy #1a2a4a
        palette.SetColorAtIndex(41, 244, 246, 251);  // alt row
        palette.SetColorAtIndex(42, 238, 242, 255);  // title bg

        IFont F(short pts, bool bold, short idx)
        {
            var f = wb.CreateFont(); f.FontHeightInPoints = pts;
            f.IsBold = bold; f.Color = idx; return f;
        }
        var fNombre = F(14, true,  40);
        var fSub    = F(10, true,  40);
        var fDet    = F(9,  false, IndexedColors.Grey50Percent.Index);
        var fHeader = F(9,  true,  IndexedColors.White.Index);
        var fData   = F(9,  false, IndexedColors.Black.Index);
        var fPie    = F(8,  false, IndexedColors.Grey50Percent.Index);

        ICellStyle S(IFont font, short bg = -1,
            HorizontalAlignment al = HorizontalAlignment.Left,
            string? fmt = null, bool border = false)
        {
            var s = wb.CreateCellStyle();
            s.SetFont(font); s.Alignment = al; s.VerticalAlignment = VerticalAlignment.Center;
            if (bg >= 0) { s.FillForegroundColor = bg; s.FillPattern = FillPattern.SolidForeground; }
            if (fmt != null) s.DataFormat = wb.CreateDataFormat().GetFormat(fmt);
            if (border) { s.BorderBottom = BorderStyle.Hair; s.BottomBorderColor = IndexedColors.Grey25Percent.Index; }
            return s;
        }

        var sHeader = S(fHeader, 40, HorizontalAlignment.Center);
        sHeader.BorderBottom = BorderStyle.Thin; sHeader.BorderTop    = BorderStyle.Thin;
        sHeader.BorderLeft   = BorderStyle.Thin; sHeader.BorderRight  = BorderStyle.Thin;

        return new ExcelStyles(
            Nombre:    S(fNombre),
            Sub:       S(fSub),
            Det:       S(fDet),
            TitleBg:   S(fSub,   42, HorizontalAlignment.Center),
            TitleDate: S(fDet,   42, HorizontalAlignment.Right),
            Header:    sHeader,
            Data:      S(fData,  -1, HorizontalAlignment.Left, null, true),
            DataAlt:   S(fData,  41, HorizontalAlignment.Left, null, true),
            Num:       S(fData,  -1, HorizontalAlignment.Right, "#,##0.0000", true),
            NumAlt:    S(fData,  41, HorizontalAlignment.Right, "#,##0.0000", true),
            Pie:       S(fPie,   -1, HorizontalAlignment.Right)
        );
    }

    private static int InsertarEncabezadoEmpresa(
        HSSFWorkbook wb, ISheet sheet, ExcelStyles st,
        byte[]? logoBytes,
        string nombre, string razonSocial,
        string direccion, string localidad,
        string cuit, string telefono,
        string titulo, int totalCols)
    {
        // Filas 0-3 para logo + datos empresa
        for (int r = 0; r < 4; r++)
        {
            var row = sheet.CreateRow(r);
            row.HeightInPoints = r == 0 ? 45f : 18f;
        }

        if (logoBytes?.Length > 0)
        {
            try
            {
                int pic = wb.AddPicture(logoBytes, PictureType.JPEG);
                var drawing = sheet.CreateDrawingPatriarch();
                var anchor  = new HSSFClientAnchor(0, 0, 0, 0, 0, 0, 2, 4);
                anchor.AnchorType = AnchorType.MoveAndResize;
                drawing.CreatePicture(anchor, pic);
            }
            catch { }
        }

        void SC(int ri, int ci, string v, ICellStyle s, int span = 0)
        {
            var row  = sheet.GetRow(ri) ?? sheet.CreateRow(ri);
            var cell = row.CreateCell(ci);
            cell.SetCellValue(v); cell.CellStyle = s;
            if (span > 0)
                sheet.AddMergedRegion(new CellRangeAddress(ri, ri, ci, ci + span - 1));
        }

        int nc = totalCols - 2; // nro de cols para merge desde col 2
        if (!string.IsNullOrEmpty(nombre))      SC(0, 2, nombre,      st.Nombre,   nc);
        if (!string.IsNullOrEmpty(razonSocial)) SC(1, 2, razonSocial, st.Sub,      nc);

        var dir = string.Join(" | ",
            new[] { direccion, localidad }.Where(s => !string.IsNullOrEmpty(s)));
        if (!string.IsNullOrEmpty(dir)) SC(2, 2, dir, st.Det, nc);

        var fis = string.Join("   ",
            new[] {
                !string.IsNullOrEmpty(cuit)     ? $"CUIT: {cuit}"    : null,
                !string.IsNullOrEmpty(telefono) ? $"Tel: {telefono}" : null
            }.Where(s => s != null));
        if (!string.IsNullOrEmpty(fis)) SC(3, 2, fis, st.Det, nc);

        // Fila título
        var tRow = sheet.CreateRow(4);
        tRow.HeightInPoints = 24f;
        var tc = tRow.CreateCell(0);
        tc.SetCellValue(titulo); tc.CellStyle = st.TitleBg;
        sheet.AddMergedRegion(new CellRangeAddress(4, 4, 0, totalCols - 2));
        var dc = tRow.CreateCell(totalCols - 1);
        dc.SetCellValue(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        dc.CellStyle = st.TitleDate;

        return 5; // siguiente fila libre
    }

    private static int InsertarCabeceraColumnas(
        ISheet sheet, ExcelStyles st, int rowIdx, string[] cols)
    {
        var hRow = sheet.CreateRow(rowIdx);
        hRow.HeightInPoints = 18f;
        for (int ci = 0; ci < cols.Length; ci++)
        {
            var cell = hRow.CreateCell(ci);
            cell.SetCellValue(cols[ci]);
            cell.CellStyle = st.Header;
        }
        return rowIdx + 1;
    }

    private static void InsertarPie(
        ISheet sheet, ExcelStyles st, int rowIdx, int total, int totalCols)
    {
        var pr = sheet.CreateRow(rowIdx);
        pr.HeightInPoints = 14f;
        var c  = pr.CreateCell(0);
        c.SetCellValue($"Total: {total} registro(s)  —  {DateTime.Now:dd/MM/yyyy HH:mm}");
        c.CellStyle = st.Pie;
        sheet.AddMergedRegion(new CellRangeAddress(rowIdx, rowIdx, 0, totalCols - 1));
    }

    private static void SetStr(IRow row, int ci, string v, ICellStyle st)
    { var c = row.CreateCell(ci); c.SetCellValue(v); c.CellStyle = st; }

    private static void SetNum(IRow row, int ci, double v, ICellStyle st)
    { var c = row.CreateCell(ci); c.SetCellValue(v); c.CellStyle = st; }
}

// DTO para la petición de exportación de movimientos (incluye descripción del producto)
public class ExportarMovimientosRequest
{
    public string                  Descripcion { get; set; } = string.Empty;
    public List<MovimientoItemDto> Filas       { get; set; } = new();
}
