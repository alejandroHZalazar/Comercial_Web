using Application.Interfaces;
using Domain.Contracts;
using Domain.DTO;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Web;

namespace Comercial_Web.Pages.Contable.AuditoriaCaja;

[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly ICajaService      _cajaService;
    private readonly IParametroService _parametroService;
    private readonly ComercialDbContext _db;

    public IndexModel(
        ICajaService       cajaService,
        IParametroService  parametroService,
        ComercialDbContext db)
    {
        _cajaService      = cajaService;
        _parametroService = parametroService;
        _db               = db;
    }

    // ── Datos para la vista ─────────────────────────────────────────────────
    public List<SelectListItem> ListaUsuarios { get; private set; } = new();

    // ── OnGetAsync ──────────────────────────────────────────────────────────
    public async Task OnGetAsync()
    {
        ListaUsuarios = await _db.Usuarios
            .Where(u => u.Baja != true)
            .OrderBy(u => u.Nombre)
            .Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text  = u.Nombre ?? ""
            })
            .ToListAsync();
    }

    // ── Helpers de parseo ───────────────────────────────────────────────────
    private static (DateTime desde, DateTime hasta) ParseFechas(string? desde, string? hasta)
    {
        var hoy   = DateTime.Today;
        var dDesde = DateTime.TryParse(desde, out var pd) ? pd : new DateTime(hoy.Year, hoy.Month, 1);
        var dHasta = DateTime.TryParse(hasta, out var ph) ? ph : hoy;
        return (dDesde, dHasta);
    }

    private static List<int> ParseUsuarioIds(string? usuarioIds)
    {
        if (string.IsNullOrWhiteSpace(usuarioIds)) return new List<int>();
        return usuarioIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
            .Where(id => id > 0)
            .ToList();
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // ── OnGetEncabezadosAsync ───────────────────────────────────────────────
    public async Task<IActionResult> OnGetEncabezadosAsync(
        string? desde, string? hasta, string? usuarioIds)
    {
        var (dDesde, dHasta) = ParseFechas(desde, hasta);
        var ids = ParseUsuarioIds(usuarioIds);

        var data = await _cajaService.GetEncabezadosAsync(ids, dDesde, dHasta);
        return new JsonResult(data, _jsonOptions);
    }

    // ── OnGetResumenAsync ───────────────────────────────────────────────────
    public async Task<IActionResult> OnGetResumenAsync(int cajaId)
    {
        var data = await _cajaService.GetResumenAsync(cajaId, 0);
        return new JsonResult(data, _jsonOptions);
    }

    // ── OnGetDetalleAsync ───────────────────────────────────────────────────
    public async Task<IActionResult> OnGetDetalleAsync(
        string? desde, string? hasta, string? usuarioIds)
    {
        var (dDesde, dHasta) = ParseFechas(desde, hasta);
        var ids = ParseUsuarioIds(usuarioIds);

        var data = await _cajaService.GetDetalleMovimientosAsync(ids, dDesde, dHasta);
        return new JsonResult(data, _jsonOptions);
    }

    // ── OnGetReporteAsync ───────────────────────────────────────────────────
    public async Task<IActionResult> OnGetReporteAsync(
        string? desde, string? hasta, string? usuarioIds, string? tipo)
    {
        var (dDesde, dHasta) = ParseFechas(desde, hasta);
        var ids = ParseUsuarioIds(usuarioIds);
        tipo = (tipo ?? "resumen").ToLowerInvariant();

        // 1. Encabezados
        var encabezados = await _cajaService.GetEncabezadosAsync(ids, dDesde, dHasta);

        // 2. Detalle (si aplica)
        List<CajaDetalleMovimientoDto>? detalle = null;
        if (tipo == "detalle" || tipo == "completo")
            detalle = await _cajaService.GetDetalleMovimientosAsync(ids, dDesde, dHasta);

        // 3. Resumen por caja (si aplica)
        Dictionary<int, List<CajaResumenItemDto>>? resumenes = null;
        if (tipo == "resumen" || tipo == "completo")
        {
            resumenes = new Dictionary<int, List<CajaResumenItemDto>>();
            foreach (var enc in encabezados)
            {
                var res = await _cajaService.GetResumenAsync(enc.CajaId, 0);
                resumenes[enc.CajaId] = res;
            }
        }

        // 4. Datos de empresa
        var empNombre = await _parametroService.ObtenerValorAsync("empresa", "nombre") ?? "";
        var empRazon  = await _parametroService.ObtenerValorAsync("empresa", "razonSocial") ?? "";
        var empTel    = await _parametroService.ObtenerValorAsync("empresa", "telefono") ?? "";

        // Logo
        var logoParam = await _parametroService.GetLogoAsync();
        string logoHtml = "";
        if (logoParam?.Imagen != null && logoParam.Imagen.Length > 0)
        {
            logoHtml = $"<img src='data:image/png;base64,{Convert.ToBase64String(logoParam.Imagen)}' style='max-height:55px;max-width:130px;' />";
        }

        // 5. Construir HTML
        var html = new StringBuilder();

        var strDesde = dDesde.ToString("dd/MM/yyyy");
        var strHasta = dHasta.ToString("dd/MM/yyyy");

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang='es'>");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset='utf-8' />");
        html.AppendLine("<title>Auditoría de Caja</title>");
        html.AppendLine("<style>");
        html.AppendLine("  * { box-sizing: border-box; margin: 0; padding: 0; }");
        html.AppendLine("  body { font-family: Arial, Helvetica, sans-serif; font-size: 11px; color: #222; background: #fff; padding: 18px 22px; }");
        html.AppendLine("  .rpt-header { display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #1a3a5c; padding-bottom: 8px; margin-bottom: 12px; }");
        html.AppendLine("  .rpt-empresa-nombre { font-size: 15px; font-weight: 700; color: #1a3a5c; }");
        html.AppendLine("  .rpt-empresa-sub { font-size: 10px; color: #555; margin-top: 2px; }");
        html.AppendLine("  .rpt-titulo { font-size: 13px; font-weight: 700; color: #1a3a5c; text-align: center; margin-bottom: 4px; }");
        html.AppendLine("  .rpt-filtros { font-size: 10px; color: #555; text-align: center; margin-bottom: 10px; }");
        html.AppendLine("  table { width: 100%; border-collapse: collapse; margin-bottom: 10px; }");
        html.AppendLine("  th { background: #1a3a5c; color: #fff; padding: 4px 6px; text-align: left; font-size: 10px; font-weight: 700; }");
        html.AppendLine("  td { padding: 3px 6px; font-size: 10px; border-bottom: 1px solid #e0e0e0; vertical-align: top; }");
        html.AppendLine("  tr:nth-child(even) td { background: #f6f8fb; }");
        html.AppendLine("  .sub-table { margin: 0 0 4px 20px; width: calc(100% - 20px); }");
        html.AppendLine("  .sub-table th { background: #2e6da4; font-size: 9px; padding: 3px 5px; }");
        html.AppendLine("  .sub-table td { font-size: 9px; padding: 2px 5px; }");
        html.AppendLine("  .sub-table tr:nth-child(even) td { background: #eef3ff; }");
        html.AppendLine("  .badge-abierta { background: #d4edda; color: #155724; padding: 1px 5px; border-radius: 3px; font-weight: 700; }");
        html.AppendLine("  .badge-cerrada { background: #f8d7da; color: #721c24; padding: 1px 5px; border-radius: 3px; font-weight: 700; }");
        html.AppendLine("  .badge-ing { background: #d4edda; color: #155724; padding: 1px 4px; border-radius: 3px; font-weight: 700; font-size: 9px; }");
        html.AppendLine("  .badge-egr { background: #f8d7da; color: #721c24; padding: 1px 4px; border-radius: 3px; font-weight: 700; font-size: 9px; }");
        html.AppendLine("  .section-title { font-size: 11px; font-weight: 700; color: #1a3a5c; margin: 12px 0 4px; border-bottom: 1px solid #c5d5e8; padding-bottom: 3px; }");
        html.AppendLine("  .num-right { text-align: right; }");
        html.AppendLine("  .print-btn { display: inline-block; margin: 10px 0 16px; padding: 6px 18px; background: #1a3a5c; color: #fff; border: none; border-radius: 5px; cursor: pointer; font-size: 11px; font-weight: 700; }");
        html.AppendLine("  .totals-row td { font-weight: 700; background: #e8eef7 !important; border-top: 2px solid #1a3a5c; }");
        html.AppendLine("  @media print {");
        html.AppendLine("    .print-btn { display: none !important; }");
        html.AppendLine("    body { padding: 0; }");
        html.AppendLine("    @page { margin: 15mm 12mm; }");
        html.AppendLine("  }");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");

        // Botón imprimir
        html.AppendLine("<button class='print-btn' onclick='window.print()'>&#128438; Imprimir / Guardar PDF</button>");

        // Encabezado empresa
        html.AppendLine("<div class='rpt-header'>");
        html.AppendLine($"  <div>{logoHtml}</div>");
        html.AppendLine("  <div style='text-align:right;'>");
        if (!string.IsNullOrWhiteSpace(empNombre))
            html.AppendLine($"    <div class='rpt-empresa-nombre'>{HE(empNombre)}</div>");
        if (!string.IsNullOrWhiteSpace(empRazon))
            html.AppendLine($"    <div class='rpt-empresa-sub'>{HE(empRazon)}</div>");
        if (!string.IsNullOrWhiteSpace(empTel))
            html.AppendLine($"    <div class='rpt-empresa-sub'>Tel: {HE(empTel)}</div>");
        html.AppendLine("  </div>");
        html.AppendLine("</div>");

        // Título del reporte
        html.AppendLine("<div class='rpt-titulo'>Auditoría de Caja</div>");
        html.AppendLine($"<div class='rpt-filtros'>Período: {HE(strDesde)} al {HE(strHasta)}</div>");

        // ── Sección: Encabezados ─────────────────────────────────────────────
        html.AppendLine("<div class='section-title'>Encabezados de Caja</div>");
        html.AppendLine("<table>");
        html.AppendLine("  <thead><tr>");
        html.AppendLine("    <th>Nro</th><th>Usuario</th><th>Apertura</th><th>Cierre</th>");
        html.AppendLine("    <th class='num-right'>Saldo Apertura</th><th class='num-right'>Saldo Cierre</th><th>Estado</th><th>Obs.</th>");
        html.AppendLine("  </tr></thead>");
        html.AppendLine("  <tbody>");

        if (encabezados.Count == 0)
        {
            html.AppendLine("    <tr><td colspan='8' style='text-align:center;color:#888;'>Sin cajas en el período seleccionado.</td></tr>");
        }
        else
        {
            foreach (var enc in encabezados)
            {
                var estadoBadge = enc.Estado == "ABIERTA"
                    ? "<span class='badge-abierta'>ABIERTA</span>"
                    : "<span class='badge-cerrada'>CERRADA</span>";

                html.AppendLine("    <tr>");
                html.AppendLine($"      <td>{enc.CajaId}</td>");
                html.AppendLine($"      <td>{HE(enc.Usuario)}</td>");
                html.AppendLine($"      <td>{FmtD(enc.FechaApertura)}</td>");
                html.AppendLine($"      <td>{FmtD(enc.FechaCierre)}</td>");
                html.AppendLine($"      <td class='num-right'>{FmtM(enc.SaldoApertura)}</td>");
                html.AppendLine($"      <td class='num-right'>{FmtM(enc.SaldoCierre)}</td>");
                html.AppendLine($"      <td>{estadoBadge}</td>");
                html.AppendLine($"      <td>{HE(enc.Observaciones)}</td>");
                html.AppendLine("    </tr>");

                // Sub-tabla de resumen si aplica
                if ((tipo == "resumen" || tipo == "completo") &&
                    resumenes != null &&
                    resumenes.TryGetValue(enc.CajaId, out var resItems) &&
                    resItems.Count > 0)
                {
                    html.AppendLine("    <tr><td colspan='8' style='padding:0;'>");
                    html.AppendLine("      <table class='sub-table'>");
                    html.AppendLine("        <thead><tr><th>Debe</th><th class='num-right'>Importe Debe</th><th>Haber</th><th class='num-right'>Importe Haber</th></tr></thead>");
                    html.AppendLine("        <tbody>");
                    foreach (var r in resItems)
                    {
                        html.AppendLine("          <tr>");
                        html.AppendLine($"            <td>{HE(r.Debe)}</td>");
                        html.AppendLine($"            <td class='num-right' style='color:#c0392b;'>{FmtM(r.ImporteDebe)}</td>");
                        html.AppendLine($"            <td>{HE(r.Haber)}</td>");
                        html.AppendLine($"            <td class='num-right' style='color:#1a6e40;'>{FmtM(r.ImporteHaber)}</td>");
                        html.AppendLine("          </tr>");
                    }
                    html.AppendLine("        </tbody>");
                    html.AppendLine("      </table>");
                    html.AppendLine("    </td></tr>");
                }
            }
        }

        html.AppendLine("  </tbody>");
        html.AppendLine("</table>");

        // ── Sección: Detalle de movimientos ─────────────────────────────────
        if ((tipo == "detalle" || tipo == "completo") && detalle != null)
        {
            html.AppendLine("<div class='section-title'>Detalle de Movimientos</div>");
            html.AppendLine("<table>");
            html.AppendLine("  <thead><tr>");
            html.AppendLine("    <th>Nro Caja</th><th>Fecha</th><th>Concepto</th><th>Tipo</th><th>Medio Pago</th><th class='num-right'>Importe</th><th>Obs.</th>");
            html.AppendLine("  </tr></thead>");
            html.AppendLine("  <tbody>");

            if (detalle.Count == 0)
            {
                html.AppendLine("    <tr><td colspan='7' style='text-align:center;color:#888;'>Sin movimientos en el período.</td></tr>");
            }
            else
            {
                decimal totIng = 0m, totEgr = 0m;
                foreach (var d in detalle)
                {
                    var tipoBadge = d.Tipo == "Ingreso"
                        ? "<span class='badge-ing'>Ingreso</span>"
                        : "<span class='badge-egr'>Egreso</span>";
                    if (d.Tipo == "Ingreso") totIng += d.Importe;
                    else totEgr += d.Importe;

                    html.AppendLine("    <tr>");
                    html.AppendLine($"      <td style='text-align:center;'>{d.NroCaja}</td>");
                    html.AppendLine($"      <td>{FmtD(d.Fecha)}</td>");
                    html.AppendLine($"      <td>{HE(d.Concepto)}</td>");
                    html.AppendLine($"      <td style='text-align:center;'>{tipoBadge}</td>");
                    html.AppendLine($"      <td>{HE(d.MedioPago)}</td>");
                    html.AppendLine($"      <td class='num-right'>{FmtM(d.Importe)}</td>");
                    html.AppendLine($"      <td>{HE(d.Observaciones)}</td>");
                    html.AppendLine("    </tr>");
                }

                // Fila de totales
                html.AppendLine("  </tbody>");
                html.AppendLine("  <tfoot>");
                html.AppendLine("    <tr class='totals-row'>");
                html.AppendLine("      <td colspan='4' style='text-align:right;'>Totales:</td>");
                html.AppendLine($"      <td style='color:#155724;'>Ingresos: {FmtM(totIng)}</td>");
                html.AppendLine($"      <td class='num-right' style='color:#721c24;'>Egresos: {FmtM(totEgr)}</td>");
                html.AppendLine("      <td></td>");
                html.AppendLine("    </tr>");
                html.AppendLine("  </tfoot>");
                html.AppendLine("</table>");
                goto afterDetalle;
            }

            html.AppendLine("  </tbody>");
            html.AppendLine("</table>");
        }
        afterDetalle:

        // Auto-print
        html.AppendLine("<script>window.onload = function(){ window.print(); };</script>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return Content(html.ToString(), "text/html", Encoding.UTF8);
    }

    // ── Helpers estáticos ───────────────────────────────────────────────────
    private static string HE(string? s) => HttpUtility.HtmlEncode(s ?? "");

    private static string FmtD(DateTime? dt) =>
        dt?.ToString("dd/MM/yyyy HH:mm") ?? "—";

    private static string FmtM(decimal? v) =>
        v.HasValue
            ? v.Value.ToString("N2", new System.Globalization.CultureInfo("es-AR"))
            : "—";
}
