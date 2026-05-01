using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Comercial_Web.Pages.Clientes.SaldosClientes;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ICuentaCorrienteService _ccService;
    private readonly ILocalidadService       _localidadService;
    private readonly IUsuarioService         _usuarioService;
    private readonly IZonaClienteService     _zonaClienteService;

    public IndexModel(
        ICuentaCorrienteService ccService,
        ILocalidadService       localidadService,
        IUsuarioService         usuarioService,
        IZonaClienteService     zonaClienteService)
    {
        _ccService          = ccService;
        _localidadService   = localidadService;
        _usuarioService     = usuarioService;
        _zonaClienteService = zonaClienteService;
    }

    // ── Listas para los filtros ──────────────────────────────────────────
    public List<SelectListItem> Provincias  { get; private set; } = new();
    public List<SelectListItem> Localidades { get; private set; } = new();
    public List<SelectListItem> Vendedores  { get; private set; } = new();
    public List<SelectListItem> Zonas       { get; private set; } = new();

    // ── Resultados ──────────────────────────────────────────────────────
    public List<SaldoClienteDto> Resultados       { get; private set; } = new();
    public bool                  BusquedaRealizada { get; private set; }

    // ── Filtros enlazados ────────────────────────────────────────────────
    [BindProperty(SupportsGet = true)] public List<int> SelProvincias  { get; set; } = new();
    [BindProperty(SupportsGet = true)] public List<int> SelLocalidades { get; set; } = new();
    [BindProperty(SupportsGet = true)] public List<int> SelVendedores  { get; set; } = new();
    [BindProperty(SupportsGet = true)] public List<int> SelZonas       { get; set; } = new();

    private async Task CargarFiltrosAsync()
    {
        // Provincias via ILocalidadService.GetProvinciasAsync()
        var provincias = await _localidadService.GetProvinciasAsync();
        Provincias = provincias
            .Where(p => p.Baja != true)
            .OrderBy(p => p.Nombre)
            .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Nombre ?? "" })
            .ToList();

        var localidades = await _localidadService.GetAllAsync();
        Localidades = localidades
            .Where(l => l.Baja != true)
            .OrderBy(l => l.Nombre)
            .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Nombre ?? "" })
            .ToList();

        var vendedores = await _usuarioService.GetAllAsync();
        Vendedores = vendedores
            .Where(u => u.Baja != true)
            .OrderBy(u => u.Nombre)
            .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Nombre ?? "" })
            .ToList();

        var zonas = await _zonaClienteService.GetAllAsync();
        Zonas = zonas
            .OrderBy(z => z.Nombre)
            .Select(z => new SelectListItem { Value = z.Id.ToString(), Text = z.Nombre ?? "" })
            .ToList();
    }

    public async Task OnGetAsync()
    {
        await CargarFiltrosAsync();

        if (Request.Query.ContainsKey("buscar"))
        {
            BusquedaRealizada = true;
            Resultados = await _ccService.GetSaldosDeudoresAsync(
                SelProvincias, SelLocalidades, SelVendedores, SelZonas);
        }
    }

    public async Task<IActionResult> OnGetExportarExcelAsync()
    {
        var datos = await _ccService.GetSaldosDeudoresAsync(
            SelProvincias, SelLocalidades, SelVendedores, SelZonas);

        var wb    = new XSSFWorkbook();
        var sheet = wb.CreateSheet("Saldos Deudores");

        // ── Estilos ──────────────────────────────────────────────────────
        var fuenteTitulo = wb.CreateFont();
        fuenteTitulo.IsBold = true;
        fuenteTitulo.FontHeightInPoints = 14;

        var fuenteHeader = wb.CreateFont();
        fuenteHeader.IsBold = true;
        fuenteHeader.Color  = IndexedColors.White.Index;

        var estTitulo = wb.CreateCellStyle();
        estTitulo.SetFont(fuenteTitulo);

        var estHeader = wb.CreateCellStyle();
        estHeader.SetFont(fuenteHeader);
        estHeader.FillForegroundColor = IndexedColors.DarkBlue.Index;
        estHeader.FillPattern         = FillPattern.SolidForeground;
        estHeader.Alignment           = HorizontalAlignment.Center;

        var estMoneda = wb.CreateCellStyle();
        estMoneda.DataFormat = wb.CreateDataFormat().GetFormat("#,##0.00");

        var estMonedaRojo = wb.CreateCellStyle();
        estMonedaRojo.DataFormat = wb.CreateDataFormat().GetFormat("#,##0.00");
        var fRojo = wb.CreateFont();
        fRojo.Color  = IndexedColors.Red.Index;
        fRojo.IsBold = true;
        estMonedaRojo.SetFont(fRojo);

        var estTotal = wb.CreateCellStyle();
        estTotal.DataFormat = wb.CreateDataFormat().GetFormat("#,##0.00");
        var fTotal = wb.CreateFont();
        fTotal.IsBold = true;
        estTotal.SetFont(fTotal);

        // ── Fila 0: Título ───────────────────────────────────────────────
        var rowTitulo = sheet.CreateRow(0);
        var cellTit   = rowTitulo.CreateCell(0);
        cellTit.SetCellValue("Clientes con Saldo Deudor — " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        cellTit.CellStyle = estTitulo;
        sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(0, 0, 0, 12));

        // ── Fila 1: vacía ────────────────────────────────────────────────

        // ── Fila 2: encabezado de columnas ───────────────────────────────
        string[] headers = {
            "N° Cliente","Nombre Comercial","Razón Social","CUIL/CUIT",
            "Dirección","Localidad","Provincia","Zona","Vendedor",
            "Teléfono","Total Debe","Total Haber","Saldo Deudor"
        };
        var rowHeader = sheet.CreateRow(2);
        for (int i = 0; i < headers.Length; i++)
        {
            var c = rowHeader.CreateCell(i);
            c.SetCellValue(headers[i]);
            c.CellStyle = estHeader;
        }

        // ── Filas de datos ───────────────────────────────────────────────
        int rowIdx = 3;
        foreach (var d in datos)
        {
            var row = sheet.CreateRow(rowIdx++);
            row.CreateCell(0).SetCellValue(d.ClienteId);
            row.CreateCell(1).SetCellValue(d.NombreComercial ?? "");
            row.CreateCell(2).SetCellValue(d.RazonSocial     ?? "");
            row.CreateCell(3).SetCellValue(d.Cuil            ?? "");

            var partes = new[] { d.Direccion }.Where(s => !string.IsNullOrWhiteSpace(s));
            row.CreateCell(4).SetCellValue(string.Join(", ", partes));
            row.CreateCell(5).SetCellValue(d.LocalidadDescripcion ?? "");
            row.CreateCell(6).SetCellValue(d.ProvinciaDescripcion ?? "");
            row.CreateCell(7).SetCellValue(d.ZonaDescripcion      ?? "");
            row.CreateCell(8).SetCellValue(d.Vendedor             ?? "");
            row.CreateCell(9).SetCellValue(d.Telefono             ?? "");

            var cDebe  = row.CreateCell(10); cDebe .SetCellValue((double)d.TotalDebe ); cDebe .CellStyle = estMoneda;
            var cHaber = row.CreateCell(11); cHaber.SetCellValue((double)d.TotalHaber); cHaber.CellStyle = estMoneda;
            var cSaldo = row.CreateCell(12); cSaldo.SetCellValue((double)d.Saldo     ); cSaldo.CellStyle = estMonedaRojo;
        }

        // ── Fila de totales ──────────────────────────────────────────────
        var rowTot = sheet.CreateRow(rowIdx);
        var cLbl   = rowTot.CreateCell(9);
        cLbl.SetCellValue("TOTAL");
        var fLbl = wb.CreateFont(); fLbl.IsBold = true;
        var estLbl = wb.CreateCellStyle(); estLbl.SetFont(fLbl);
        cLbl.CellStyle = estLbl;

        void CeldaTotal(int col, decimal val)
        {
            var ct = rowTot.CreateCell(col);
            ct.SetCellValue((double)val);
            ct.CellStyle = estTotal;
        }
        CeldaTotal(10, datos.Sum(d => d.TotalDebe));
        CeldaTotal(11, datos.Sum(d => d.TotalHaber));
        CeldaTotal(12, datos.Sum(d => d.Saldo));

        // ── Ancho automático ─────────────────────────────────────────────
        for (int i = 0; i <= 12; i++) sheet.AutoSizeColumn(i);

        using var ms = new MemoryStream();
        wb.Write(ms);
        var bytes = ms.ToArray();

        var nombreArchivo = $"SaldosDeudores_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            nombreArchivo);
    }
}
