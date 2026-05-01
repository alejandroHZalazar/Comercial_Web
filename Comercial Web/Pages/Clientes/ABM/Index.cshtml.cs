using Application.Interfaces;
using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;
using System.Text;
using static Domain.DTO.ClienteDTO;

namespace Comercial_Web.Pages.Clientes.ABM
{
    [Authorize]
    [IgnoreAntiforgeryToken]   // necesario para los endpoints JSON (POST con application/json)
    public class IndexModel : PageModel
    {
        private readonly IParametroService       _parametroService;
        private readonly IClienteService         _clienteService;
        private readonly ICondicionIvaService    _condicionIvaService;
        private readonly IUsuarioService         _usuarioService;
        private readonly ILocalidadService       _localidadService;
        private readonly IZonaClienteService     _zonaClienteService;
        private readonly ICuentaCorrienteService _ccService;
        private readonly ICobroService           _cobroService;
        private readonly INotaCreditoService     _ncService;

        public IndexModel(IParametroService parametroService, IClienteService clienteService,
            ICondicionIvaService condicionIvaService, IUsuarioService usuarioService,
            ILocalidadService localidadService, IZonaClienteService zonaClienteService,
            ICuentaCorrienteService ccService, ICobroService cobroService,
            INotaCreditoService ncService)
        {
            _parametroService    = parametroService;
            _clienteService      = clienteService;
            _condicionIvaService = condicionIvaService;
            _usuarioService      = usuarioService;
            _localidadService    = localidadService;
            _zonaClienteService  = zonaClienteService;
            _ccService           = ccService;
            _cobroService        = cobroService;
            _ncService           = ncService;
        }

        public List<Cliente> Clientes { get; set; } = new();
        public List<SelectListItem> CondicionIvas { get; set; } = new();
        public List<SelectListItem> Vendedores    { get; set; } = new();
        public List<SelectListItem> Localidades   { get; set; } = new();
        public List<SelectListItem> Zonas         { get; set; } = new();

        /// <summary>1 si el módulo Cuenta Corriente está activo para clientes</summary>
        public int LlevaCC { get; private set; }

        [BindProperty]
        public Cliente Cliente { get; set; } = new();

        // ----------------------------------------------------------------
        public async Task OnGetAsync()
        {
            var filtroDefecto = await ObtenerIndiceBusquedaDefectoAsync();
            ViewData["FiltroDefecto"] = filtroDefecto;

            var lleva = await _parametroService.ObtenerValorAsync("clientes", "llevaCC");
            LlevaCC = lleva == "1" ? 1 : 0;

            await CargarCombosAsync();
            Clientes = await _clienteService.GetAllAsync();
        }

        private async Task CargarCombosAsync()
        {
            CondicionIvas = (await _condicionIvaService.GetAllAsync())
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Descripcion }).ToList();

            Vendedores = (await _usuarioService.GetAllAsync())
                .Where(u => u.Baja != true)
                .Select(v => new SelectListItem { Value = v.Id.ToString(), Text = v.Nombre }).ToList();

            Localidades = (await _localidadService.GetAllAsync())
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Nombre }).ToList();

            Zonas = (await _zonaClienteService.GetAllAsync())
                .Select(z => new SelectListItem { Value = z.Id.ToString(), Text = z.Nombre }).ToList();
        }

        private async Task<int> ObtenerIndiceBusquedaDefectoAsync()
        {
            var parametro = await _parametroService.ObtenerValorAsync("clientes", "indiceBusqueda");
            return int.TryParse(parametro, out var indice) ? indice : 0;
        }

        // ----------------------------------------------------------------
        public async Task<IActionResult> OnGetBuscarClientesAsync(int tipo, string valor)
        {
            valor = (valor ?? "").ToLower();

            IEnumerable<Cliente> clientes = tipo switch
            {
                0 or 1 or 2 => await _clienteService.BuscarAsync(tipo, valor),
                3            => await _clienteService.BuscarPorLocalidadAsync(valor),
                4            => await _clienteService.BuscarPorZonaAsync(valor),
                _            => Enumerable.Empty<Cliente>()
            };

            var html = new StringBuilder();
            foreach (var c in clientes)
            {
                html.AppendLine(
                    $"<tr onclick=\"mostrarDetalle({c.Id})\" data-nombre=\"{HE(c.NombreComercial?.ToUpper())}\">" +
                    $"<td><code style=\"font-size:.75rem;\">{c.Id}</code></td>" +
                    $"<td style=\"font-weight:600;\">{HE(c.NombreComercial)}</td>" +
                    $"<td style=\"font-size:.79rem;color:#6c757d;\">{HE(c.RazonSocial)}</td>" +
                    $"<td class=\"text-center\">" +
                    $"<button class=\"btn-cli-edit\" title=\"Editar\" onclick=\"event.stopPropagation();abrirModalEditar({c.Id})\">" +
                    "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"13\" height=\"13\" fill=\"currentColor\" viewBox=\"0 0 16 16\"><path d=\"M12.146.146a.5.5 0 0 1 .708 0l3 3a.5.5 0 0 1 0 .708l-10 10a.5.5 0 0 1-.168.11l-5 2a.5.5 0 0 1-.65-.65l2-5a.5.5 0 0 1 .11-.168l10-10zM11.207 2.5 13.5 4.793 14.793 3.5 12.5 1.207 11.207 2.5zm1.586 3L10.5 3.207 4 9.707V10h.5a.5.5 0 0 1 .5.5v.5h.5a.5.5 0 0 1 .5.5v.5h.293l6.5-6.5zm-9.761 5.175-.106.106-1.528 3.821 3.821-1.528.106-.106A.5.5 0 0 1 5 12.5V12h-.5a.5.5 0 0 1-.5-.5V11h-.5a.5.5 0 0 1-.468-.325z\"/></svg>" +
                    $"</button>" +
                    $"</td></tr>");
            }

            return Content(html.ToString(), "text/html");
        }

        // ----------------------------------------------------------------
        public async Task<IActionResult> OnGetDetalleAsync(int id)
        {
            var c = await _clienteService.traerDetalleAsync(id);
            if (c is null)
                return Content("<p style=\"color:#e74a3b;padding:.75rem;\">Cliente no encontrado.</p>", "text/html");

            var html = new StringBuilder();
            html.Append(DetField("Código", $"<code>{c.Id}</code>"));
            html.Append(DetField("Nombre Comercial", $"<strong>{HE(c.NombreComercial)}</strong>"));
            html.Append(DetField("Razón Social", HE(c.RazonSocial)));
            html.Append(DetField("CUIL/CUIT", HE(c.Cuil)));
            html.Append(DetField("Dirección", HE(c.Direccion)));
            html.Append(DetFieldRow("Localidad", HE(c.LocalidadDescripcion), "Provincia", HE(c.ProvinciaDescripcion)));
            html.Append(DetField("Zona", HE(c.ZonaDescripcion)));
            html.Append(DetField("Email",
                !string.IsNullOrWhiteSpace(c.Email)
                    ? $"<a href=\"mailto:{HE(c.Email)}\" style=\"color:#4e73df;text-decoration:none;\">{HE(c.Email)}</a>"
                    : "—"));
            html.Append(DetFieldRow("Teléfono", HE(c.Telefono), "Celular", HE(c.Celular)));
            html.Append(DetField("Contacto", HE(c.Contacto)));
            html.Append(DetField("Condición IVA", HE(c.CondicionIva)));
            html.Append(DetField("Vendedor", HE(c.Vendedor)));

            // Botones acción
            var lleva = await _parametroService.ObtenerValorAsync("clientes", "llevaCC");
            var btnCC = lleva == "1"
                ? $"<button class=\"btn-cli btn-cli-info\" onclick=\"abrirModalCC({c.Id}, '{System.Net.WebUtility.HtmlEncode(c.NombreComercial ?? "")}')\">" +
                  "<svg xmlns=\"http://www.w3.org/2000/svg\" fill=\"currentColor\" viewBox=\"0 0 16 16\" width=\"14\" height=\"14\"><path d=\"M0 4a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v1H0V4zm0 3h16v5a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V7zm3 2a.5.5 0 0 0 0 1h1a.5.5 0 0 0 0-1H3zm2.5 0a.5.5 0 0 0 0 1h5a.5.5 0 0 0 0-1h-5z\"/></svg>" +
                  " Cta. Cte.</button>" +
                  $"<button class=\"btn-cli btn-cli-cobro\" onclick=\"abrirModalCobro({c.Id}, '{System.Net.WebUtility.HtmlEncode(c.NombreComercial ?? "")}')\">" +
                  "<svg xmlns=\"http://www.w3.org/2000/svg\" fill=\"currentColor\" viewBox=\"0 0 16 16\" width=\"14\" height=\"14\"><path d=\"M4 10.781c.148 1.667 1.513 2.85 3.591 3.003V15h1.043v-1.216c2.27-.179 3.678-1.438 3.678-3.3 0-1.59-.947-2.51-2.956-3.028l-.722-.187V3.467c1.122.11 1.879.714 2.07 1.616h1.47c-.166-1.6-1.54-2.748-3.54-2.875V1H7.591v1.233c-1.939.23-3.27 1.472-3.27 3.156 0 1.454.966 2.483 2.661 2.917l.61.162v4.031c-1.149-.17-1.94-.8-2.131-1.718H4zm3.391-3.836c-1.043-.263-1.6-.825-1.6-1.616 0-.944.704-1.641 1.8-1.828v3.495l-.2-.051zm1.591 1.872c1.287.323 1.852.859 1.852 1.769 0 1.097-.826 1.828-2.2 1.939V8.73l.348.086z\"/></svg>" +
                  " Cobrar</button>"
                : "";

            html.Append(
                "<div style=\"padding:.75rem;display:flex;gap:.5rem;justify-content:flex-end;flex-wrap:wrap;border-top:1px solid #f0f0f0;margin-top:.25rem;\">" +
                btnCC +
                $"<button class=\"btn-cli btn-cli-warning\" onclick=\"abrirModalEditar({c.Id})\">" +
                "<svg xmlns=\"http://www.w3.org/2000/svg\" fill=\"currentColor\" viewBox=\"0 0 16 16\" width=\"14\" height=\"14\"><path d=\"M12.146.146a.5.5 0 0 1 .708 0l3 3a.5.5 0 0 1 0 .708l-10 10a.5.5 0 0 1-.168.11l-5 2a.5.5 0 0 1-.65-.65l2-5a.5.5 0 0 1 .11-.168l10-10zM11.207 2.5 13.5 4.793 14.793 3.5 12.5 1.207 11.207 2.5zm1.586 3L10.5 3.207 4 9.707V10h.5a.5.5 0 0 1 .5.5v.5h.5a.5.5 0 0 1 .5.5v.5h.293l6.5-6.5zm-9.761 5.175-.106.106-1.528 3.821 3.821-1.528.106-.106A.5.5 0 0 1 5 12.5V12h-.5a.5.5 0 0 1-.5-.5V11h-.5a.5.5 0 0 1-.468-.325z\"/></svg>" +
                " Editar</button>" +
                $"<button class=\"btn-cli btn-cli-danger\" onclick=\"eliminarCliente({c.Id})\">" +
                "<svg xmlns=\"http://www.w3.org/2000/svg\" fill=\"currentColor\" viewBox=\"0 0 16 16\" width=\"14\" height=\"14\"><path d=\"M5.5 5.5A.5.5 0 0 1 6 6v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5zm2.5 0a.5.5 0 0 1 .5.5v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5zm3 .5a.5.5 0 0 0-1 0v6a.5.5 0 0 0 1 0V6z\"/><path fill-rule=\"evenodd\" d=\"M14.5 3a1 1 0 0 1-1 1H13v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V4h-.5a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1H6a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1h3.5a1 1 0 0 1 1 1v1zM4.118 4 4 4.059V13a1 1 0 0 0 1 1h6a1 1 0 0 0 1-1V4.059L11.882 4H4.118zM2.5 3V2h11v1h-11z\"/></svg>" +
                " Eliminar</button>" +
                "</div>");

            return Content(html.ToString(), "text/html");
        }

        // ----------------------------------------------------------------
        public async Task<IActionResult> OnPostGuardarAsync()
        {
            if (!ModelState.IsValid)
            {
                await CargarCombosAsync();
                return Page();
            }

            if (Cliente.Id == 0)
                await _clienteService.CreateAsync(Cliente);
            else
                await _clienteService.UpdateAsync(Cliente);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEliminarAsync([FromForm] int id)
        {
            await _clienteService.DeleteAsync(id);
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnGetClienteAsync(int id)
        {
            var cliente = await _clienteService.traerDetalleAsync(id);
            if (cliente is null) return NotFound();
            return new JsonResult(cliente);
        }

        // ----------------------------------------------------------------
        // EXPORTAR EXCEL — reporte HTML con MIME Excel
        // ----------------------------------------------------------------
        public async Task<IActionResult> OnGetExportarExcelAsync()
        {
            var clientes = await _clienteService.GetAllConDetalleAsync();

            // Empresa
            var empNombre  = await _parametroService.ObtenerValorAsync("empresa", "nombre")      ?? "";
            var empRazon   = await _parametroService.ObtenerValorAsync("empresa", "razonSocial") ?? "";
            var empTel     = await _parametroService.ObtenerValorAsync("empresa", "telefono")    ?? "";
            var empEmail   = await _parametroService.ObtenerValorAsync("empresa", "email")       ?? "";

            // Logo → base64  (Excel ignora CSS: usar atributos width/height explícitos)
            string logoHtml = "";
            var logoParam = await _parametroService.GetLogoAsync();
            if (logoParam?.Imagen != null && logoParam.Imagen.Length > 0)
            {
                string mime = "image/png";
                var img = logoParam.Imagen;
                if (img.Length > 1)
                {
                    if      (img[0] == 0xFF && img[1] == 0xD8) mime = "image/jpeg";
                    else if (img[0] == 0x42 && img[1] == 0x4D) mime = "image/bmp";
                    else if (img[0] == 0x47 && img[1] == 0x49) mime = "image/gif";
                }
                // width/height HTML attributes son los que Excel respeta; style es solo para browsers
                logoHtml = $"<img src=\"data:{mime};base64,{Convert.ToBase64String(img)}\" " +
                           "width=\"90\" height=\"45\" style=\"width:90px;height:45px;\" />";
            }

            var fechaGen = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            var nroRow   = 0;

            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"UTF-8\">");
            html.AppendLine("<title>Listado de Clientes</title>");
            html.AppendLine("<style>");
            html.AppendLine("body{font-family:Arial,Helvetica,sans-serif;font-size:8.5pt;color:#222;margin:0;padding:0;}");
            html.AppendLine("table{border-collapse:collapse;}");
            html.AppendLine(".data-table{width:100%;}");
            html.AppendLine(".data-table thead tr{background:#1a2a4a;}");
            html.AppendLine(".data-table th{padding:4px 6px;color:#fff;font-size:6.5pt;text-transform:uppercase;letter-spacing:.3px;text-align:left;white-space:nowrap;}");
            html.AppendLine(".data-table td{padding:3px 6px;font-size:7.5pt;border-bottom:1px solid #eee;vertical-align:top;}");
            html.AppendLine(".data-table tbody tr:nth-child(even) td{background:#f5f8ff;}");
            html.AppendLine(".td-nro{text-align:right;color:#888;}");
            html.AppendLine(".doc-foot{font-size:7pt;color:#aaa;text-align:center;border-top:1px solid #eee;padding-top:4px;margin-top:8px;}");
            html.AppendLine("</style></head><body>");

            // ── Header: tabla con 2 columnas (Excel sí respeta tablas)
            html.AppendLine("<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-bottom:2px solid #1a2a4a;margin-bottom:8px;\">");
            html.AppendLine("<tr>");

            // Columna izquierda: logo + datos empresa
            html.AppendLine("  <td width=\"55%\" valign=\"middle\" style=\"padding:6px 8px 8px;\">");
            html.AppendLine("    <table cellpadding=\"0\" cellspacing=\"0\"><tr>");
            if (!string.IsNullOrEmpty(logoHtml))
                html.AppendLine($"      <td valign=\"middle\" style=\"padding-right:10px;\">{logoHtml}</td>");
            html.AppendLine("      <td valign=\"middle\">");
            html.AppendLine($"        <div style=\"font-size:10pt;font-weight:bold;color:#1a2a4a;\">{HE(empNombre)}</div>");
            if (!string.IsNullOrEmpty(empRazon))  html.AppendLine($"        <div style=\"font-size:7.5pt;color:#555;\">{HE(empRazon)}</div>");
            if (!string.IsNullOrEmpty(empTel))    html.AppendLine($"        <div style=\"font-size:7.5pt;color:#555;\">Tel: {HE(empTel)}</div>");
            if (!string.IsNullOrEmpty(empEmail))  html.AppendLine($"        <div style=\"font-size:7.5pt;color:#555;\">Email: {HE(empEmail)}</div>");
            html.AppendLine("      </td>");
            html.AppendLine("    </tr></table>");
            html.AppendLine("  </td>");

            // Columna derecha: título del reporte
            html.AppendLine("  <td width=\"45%\" valign=\"middle\" align=\"right\" style=\"padding:6px 8px 8px;\">");
            html.AppendLine("    <div style=\"font-size:13pt;font-weight:bold;color:#1a2a4a;\">Listado de Clientes</div>");
            html.AppendLine($"   <div style=\"font-size:7.5pt;color:#888;\">Generado el {fechaGen} &nbsp;&mdash;&nbsp; {clientes.Count} registros</div>");
            html.AppendLine("  </td>");

            html.AppendLine("</tr></table>");

            // Tabla de datos
            html.AppendLine("<table class=\"data-table\" width=\"100%\">");
            html.AppendLine("<thead><tr>");
            html.AppendLine("  <th class=\"td-nro\">Nro</th>");
            html.AppendLine("  <th>Nombre Comercial</th>");
            html.AppendLine("  <th>Razón Social</th>");
            html.AppendLine("  <th>CUIL/CUIT</th>");
            html.AppendLine("  <th>Dirección</th>");
            html.AppendLine("  <th>Email</th>");
            html.AppendLine("  <th>Teléfono</th>");
            html.AppendLine("  <th>Celular</th>");
            html.AppendLine("  <th>Contacto</th>");
            html.AppendLine("  <th>Cond. IVA</th>");
            html.AppendLine("  <th>Vendedor</th>");
            html.AppendLine("  <th>Localidad</th>");
            html.AppendLine("  <th>Provincia</th>");
            html.AppendLine("  <th>Zona</th>");
            html.AppendLine("</tr></thead><tbody>");

            foreach (var c in clientes)
            {
                nroRow++;
                html.Append("<tr>");
                html.Append($"<td class=\"td-nro\">{nroRow}</td>");
                html.Append($"<td>{HE(c.NombreComercial)}</td>");
                html.Append($"<td>{HE(c.RazonSocial)}</td>");
                html.Append($"<td>{HE(c.Cuil)}</td>");
                html.Append($"<td>{HE(c.Direccion)}</td>");
                html.Append($"<td>{HE(c.Email)}</td>");
                html.Append($"<td>{HE(c.Telefono)}</td>");
                html.Append($"<td>{HE(c.Celular)}</td>");
                html.Append($"<td>{HE(c.Contacto)}</td>");
                html.Append($"<td>{HE(c.CondicionIva)}</td>");
                html.Append($"<td>{HE(c.Vendedor)}</td>");
                html.Append($"<td>{HE(c.LocalidadDescripcion)}</td>");
                html.Append($"<td>{HE(c.ProvinciaDescripcion)}</td>");
                html.Append($"<td>{HE(c.ZonaDescripcion)}</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</tbody></table>");
            html.AppendLine($"<div class=\"doc-foot\">Generado el {fechaGen} &mdash; {HE(empNombre)}</div>");
            html.AppendLine("</body></html>");

            var bytes = Encoding.UTF8.GetBytes(html.ToString());
            return File(bytes, "application/vnd.ms-excel",
                $"Clientes_{DateTime.Now:ddMMyyyyHHmm}.xls");
        }

        // ----------------------------------------------------------------
        // CUENTA CORRIENTE
        // ----------------------------------------------------------------
        public async Task<IActionResult> OnGetCuentaCorrienteAsync(int id)
        {
            var movs = await _ccService.GetMovimientosAsync(id);

            if (!movs.Any())
                return Content(
                    "<div style=\"text-align:center;padding:2.5rem 1rem;color:#adb5bd;\">" +
                    "<svg xmlns='http://www.w3.org/2000/svg' fill='currentColor' viewBox='0 0 16 16' width='32' height='32' style='margin-bottom:.5rem;display:block;margin-left:auto;margin-right:auto;'>" +
                    "<path d='M0 4a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v1H0V4zm0 3h16v5a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V7z'/></svg>" +
                    "<div style='font-size:.88rem;'>Sin movimientos registrados</div></div>",
                    "text/html");

            // ── Totales calculados sumando columnas Debe y Haber
            var totalDebe  = movs.Sum(m => m.Debe);
            var totalHaber = movs.Sum(m => m.Haber);
            // Saldo neto: positivo = el cliente debe; negativo = saldo a favor del cliente
            var saldoNeto  = totalDebe - totalHaber;

            var html = new StringBuilder();

            html.AppendLine("<div style='overflow-x:auto;'>");
            html.AppendLine("<table class='cc-table' style='width:100%;border-collapse:collapse;'>");
            html.AppendLine("<thead><tr>");
            html.AppendLine("  <th style='width:90px;'>Fecha</th>");
            html.AppendLine("  <th>Movimiento</th>");
            html.AppendLine("  <th>Nro. Referencia</th>");
            html.AppendLine("  <th style='text-align:right;width:100px;'>Debe</th>");
            html.AppendLine("  <th style='text-align:right;width:100px;'>Haber</th>");
            html.AppendLine("  <th style='text-align:right;width:100px;'>Saldo</th>");
            html.AppendLine("  <th style='width:36px;'></th>");
            html.AppendLine("</tr></thead><tbody>");

            foreach (var m in movs)
            {
                var rowStyle = m.Debe > 0
                    ? "background:#fff8f8;"
                    : m.Haber > 0 ? "background:#f8fff9;" : "";

                var saldoFilaColor = m.Saldo > 0
                    ? "color:#e74a3b;font-weight:600;"
                    : m.Saldo < 0 ? "color:#1cc88a;font-weight:600;" : "color:#2d3748;";

                // Para filas cobro: clickeable + atributo data-cobro-id
                var trExtra = m.EsCobro && m.CobroId.HasValue
                    ? $" class='cc-fila-cobro' data-cobro-id='{m.CobroId}' style='{rowStyle}cursor:pointer;'"
                    : $" style='{rowStyle}'";

                html.AppendLine($"<tr{trExtra}>");
                html.AppendLine($"  <td style='white-space:nowrap;font-size:.78rem;'>{m.Fecha:dd/MM/yyyy}</td>");
                html.AppendLine($"  <td>{HE(m.Movimiento)}</td>");
                html.AppendLine($"  <td style='font-size:.78rem;color:#6c757d;'>{HE(m.NumeroReferencia)}</td>");
                html.AppendLine($"  <td style='text-align:right;color:#e74a3b;'>{(m.Debe > 0 ? m.Debe.ToString("N2") : "")}</td>");
                html.AppendLine($"  <td style='text-align:right;color:#1cc88a;'>{(m.Haber > 0 ? m.Haber.ToString("N2") : "")}</td>");
                html.AppendLine($"  <td style='text-align:right;{saldoFilaColor}'>{m.Saldo.ToString("N2")}</td>");

                // Botón reimprimir solo para cobros
                if (m.EsCobro && m.CobroId.HasValue)
                    html.AppendLine($"  <td style='text-align:center;padding:2px;'>" +
                        $"<button class='btn-cc-reprint' title='Reimprimir recibo' onclick='event.stopPropagation();reimprimirRecibo({m.CobroId})'>" +
                        "<svg xmlns='http://www.w3.org/2000/svg' fill='currentColor' viewBox='0 0 16 16' width='13' height='13'>" +
                        "<path d='M2.5 8a.5.5 0 1 0 0-1 .5.5 0 0 0 0 1z'/>" +
                        "<path d='M5 1a2 2 0 0 0-2 2v2H2a2 2 0 0 0-2 2v3a2 2 0 0 0 2 2h1v1a2 2 0 0 0 2 2h6a2 2 0 0 0 2-2v-1h1a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-1V3a2 2 0 0 0-2-2H5zM4 3a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v2H4V3zm1 5a2 2 0 0 0-2 2v1H2a1 1 0 0 1-1-1V7a1 1 0 0 1 1-1h12a1 1 0 0 1 1 1v3a1 1 0 0 1-1 1h-1v-1a2 2 0 0 0-2-2H5zm7 2v3a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1v-3a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1z'/>" +
                        "</svg></button></td>");
                else
                    html.AppendLine("  <td></td>");

                html.AppendLine("</tr>");
            }

            // ── Fila de totales dentro de la tabla
            var saldoNetoColor = saldoNeto > 0 ? "color:#e74a3b;font-weight:700;"
                               : saldoNeto < 0 ? "color:#1cc88a;font-weight:700;"
                               : "color:#2d3748;font-weight:700;";

            html.AppendLine("<tr style='border-top:2px solid #1a2a4a;background:#f0f4ff;'>");
            html.AppendLine("  <td colspan='3' style='font-weight:700;font-size:.76rem;text-transform:uppercase;letter-spacing:.4px;color:#1a2a4a;padding:.5rem .65rem;'>Totales</td>");
            html.AppendLine($"  <td style='text-align:right;font-weight:700;color:#e74a3b;padding:.5rem .65rem;'>{totalDebe.ToString("N2")}</td>");
            html.AppendLine($"  <td style='text-align:right;font-weight:700;color:#1cc88a;padding:.5rem .65rem;'>{totalHaber.ToString("N2")}</td>");
            html.AppendLine($"  <td style='text-align:right;{saldoNetoColor}padding:.5rem .65rem;'>{saldoNeto.ToString("N2")}</td>");
            html.AppendLine("  <td></td>");
            html.AppendLine("</tr>");

            html.AppendLine("</tbody></table></div>");

            // ── Resumen final debajo de la tabla
            var saldoLabel    = saldoNeto > 0 ? "Deuda pendiente" : saldoNeto < 0 ? "Saldo a favor" : "Sin saldo pendiente";
            var resumenStyle  = saldoNeto > 0
                ? "background:#fff0f0;border-top:2px solid #e74a3b;"
                : saldoNeto < 0 ? "background:#f0fff4;border-top:2px solid #1cc88a;"
                : "background:#f8f9fc;border-top:2px solid #e3e6f0;";
            var saldoFinalColor = saldoNeto > 0 ? "color:#e74a3b;" : saldoNeto < 0 ? "color:#1cc88a;" : "color:#2d3748;";

            html.AppendLine(
                $"<div style='display:flex;justify-content:space-between;align-items:center;" +
                $"{resumenStyle}padding:.7rem 1rem;'>" +
                $"<span style='font-size:.75rem;font-weight:700;text-transform:uppercase;letter-spacing:.5px;color:#4e73df;'>{saldoLabel}</span>" +
                $"<span style='font-size:1.1rem;font-weight:700;{saldoFinalColor}'>{saldoNeto.ToString("C2")}</span>" +
                "</div>");

            return Content(html.ToString(), "text/html");
        }

        /// <summary>Genera el reporte PDF (imprimible) del estado de cuenta corriente del cliente.</summary>
        public async Task<IActionResult> OnGetEstadoCCAsync(int id)
        {
            var estado  = await _ccService.GetEstadoCCAsync(id);
            var cliente = await _clienteService.traerDetalleAsync(id);
            if (cliente == null) return NotFound();

            // Datos de empresa
            var empNombre = await _parametroService.ObtenerValorAsync("empresa", "nombre")      ?? "";
            var empRazon  = await _parametroService.ObtenerValorAsync("empresa", "razonSocial") ?? "";
            var empDir    = await _parametroService.ObtenerValorAsync("empresa", "direccion")   ?? "";
            var empTel    = await _parametroService.ObtenerValorAsync("empresa", "telefono")    ?? "";

            // Logo
            string logoHtml = "";
            var logoParam = await _parametroService.GetLogoAsync();
            if (logoParam?.Imagen != null && logoParam.Imagen.Length > 0)
            {
                var img  = logoParam.Imagen;
                string mime = "image/png";
                if (img.Length > 1)
                {
                    if      (img[0] == 0xFF && img[1] == 0xD8) mime = "image/jpeg";
                    else if (img[0] == 0x42 && img[1] == 0x4D) mime = "image/bmp";
                    else if (img[0] == 0x47 && img[1] == 0x49) mime = "image/gif";
                }
                logoHtml = $"<img src='data:{mime};base64,{Convert.ToBase64String(img)}' " +
                           "width='100' height='50' style='width:100px;height:50px;object-fit:contain;' />";
            }

            var sf = estado.SaldoFinal;
            var sfLabel = sf > 0 ? "DEUDA PENDIENTE" : sf < 0 ? "SALDO A FAVOR" : "SIN SALDO PENDIENTE";
            var sfColor = sf > 0 ? "#c0392b" : sf < 0 ? "#1a8754" : "#2d3748";

            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html><html lang='es'><head><meta charset='UTF-8'>");
            html.AppendLine($"<title>Estado de Cuenta Corriente — {HE(cliente.NombreComercial)}</title>");
            html.AppendLine(@"<style>
*{box-sizing:border-box;margin:0;padding:0;}
body{font-family:Arial,Helvetica,sans-serif;font-size:9pt;color:#222;background:#fff;}
.page{max-width:800px;margin:0 auto;padding:8mm 10mm;}
.header-table{width:100%;border-collapse:collapse;border-bottom:2px solid #1a2a4a;margin-bottom:5mm;padding-bottom:3mm;}
.header-table td{vertical-align:middle;padding:3px 6px;}
.emp-name{font-size:11pt;font-weight:bold;color:#1a2a4a;}
.emp-sub{font-size:8pt;color:#555;line-height:1.6;}
.rep-title{font-size:15pt;font-weight:bold;color:#1a2a4a;text-align:right;}
.rep-sub{font-size:8pt;color:#777;text-align:right;margin-top:2px;}
.section{margin-bottom:4mm;}
.section-title{font-size:7pt;font-weight:bold;text-transform:uppercase;letter-spacing:.5px;
               color:#fff;background:#1a2a4a;padding:3px 8px;margin-bottom:2mm;}
.info-table{width:100%;border-collapse:collapse;}
.info-table td{padding:2px 6px;font-size:8.5pt;border-bottom:1px solid #f0f0f0;}
.info-table td:first-child{font-weight:bold;color:#4e73df;width:130px;font-size:7.5pt;text-transform:uppercase;}
.mov-table{width:100%;border-collapse:collapse;margin-top:1mm;}
.mov-table thead tr{background:#1a2a4a;}
.mov-table th{padding:4px 6px;color:#fff;font-size:7pt;text-transform:uppercase;letter-spacing:.3px;text-align:left;}
.mov-table th.r{text-align:right;}
.mov-table td{padding:3px 6px;font-size:8.5pt;border-bottom:1px solid #eee;}
.mov-table td.r{text-align:right;}
.mov-table tr.debe{background:#fff8f8;}
.mov-table tr.haber{background:#f8fff9;}
.mov-table tr.ant{background:#fffbf0;}
.mov-table tr.tot{background:#f0f4ff;border-top:2px solid #1a2a4a;}
.mov-table tr.tot td{font-weight:700;padding:4px 6px;}
.saldo-box{margin-top:4mm;padding:6px 10px;border-radius:4px;display:flex;
           justify-content:space-between;align-items:center;}
.btn-print{position:fixed;bottom:14px;right:14px;background:#1a2a4a;color:#fff;
           border:none;padding:8px 18px;border-radius:6px;font-size:9pt;cursor:pointer;}
@@media print{.btn-print{display:none;}body{font-size:8.5pt;}}
</style></head><body>");

            html.AppendLine("<div class='page'>");

            // ── Encabezado empresa ───────────────────────────────────────────
            html.AppendLine("<table class='header-table'><tr>");
            html.AppendLine($"<td style='width:120px;'>{logoHtml}</td>");
            html.AppendLine("<td>");
            html.AppendLine($"<div class='emp-name'>{HE(empNombre)}</div>");
            html.AppendLine($"<div class='emp-sub'>{HE(empRazon)}</div>");
            if (!string.IsNullOrWhiteSpace(empDir))
                html.AppendLine($"<div class='emp-sub'>{HE(empDir)}</div>");
            if (!string.IsNullOrWhiteSpace(empTel))
                html.AppendLine($"<div class='emp-sub'>Tel: {HE(empTel)}</div>");
            html.AppendLine("</td>");
            html.AppendLine("<td style='text-align:right;vertical-align:top;padding-top:4px;'>");
            html.AppendLine("<div class='rep-title'>Estado de Cuenta</div>");
            html.AppendLine($"<div class='rep-sub'>Emisión: {DateTime.Now:dd/MM/yyyy HH:mm}</div>");
            html.AppendLine($"<div class='rep-sub'>Período: últimos 6 meses</div>");
            html.AppendLine("</td></tr></table>");

            // ── Datos del cliente ────────────────────────────────────────────
            html.AppendLine("<div class='section'>");
            html.AppendLine("<div class='section-title'>Datos del Cliente</div>");
            html.AppendLine("<table class='info-table'>");
            html.AppendLine($"<tr><td>Nombre Comercial</td><td>{HE(cliente.NombreComercial)}</td></tr>");
            if (!string.IsNullOrWhiteSpace(cliente.RazonSocial))
                html.AppendLine($"<tr><td>Razón Social</td><td>{HE(cliente.RazonSocial)}</td></tr>");
            var partesDireccion  = new[] { cliente.Direccion, cliente.LocalidadDescripcion, cliente.ProvinciaDescripcion }
                                       .Where(s => !string.IsNullOrWhiteSpace(s));
            var direccionCompleta = string.Join(", ", partesDireccion);
            if (!string.IsNullOrWhiteSpace(direccionCompleta))
                html.AppendLine($"<tr><td>Dirección</td><td>{HE(direccionCompleta)}</td></tr>");
            if (!string.IsNullOrWhiteSpace(cliente.Telefono))
                html.AppendLine($"<tr><td>Teléfono</td><td>{HE(cliente.Telefono)}</td></tr>");
            html.AppendLine("</table></div>");

            // ── Movimientos ──────────────────────────────────────────────────
            html.AppendLine("<div class='section'>");
            html.AppendLine("<div class='section-title'>Movimientos de Cuenta Corriente</div>");
            html.AppendLine("<table class='mov-table'><thead><tr>");
            html.AppendLine("<th style='width:85px;'>Fecha</th>");
            html.AppendLine("<th>Movimiento</th>");
            html.AppendLine("<th>Nro. Referencia</th>");
            html.AppendLine("<th class='r' style='width:95px;'>Debe</th>");
            html.AppendLine("<th class='r' style='width:95px;'>Haber</th>");
            html.AppendLine("<th class='r' style='width:95px;'>Saldo</th>");
            html.AppendLine("</tr></thead><tbody>");

            // Fila de saldo anterior si corresponde
            if (estado.TieneSaldoAnterior)
            {
                var saColor = estado.SaldoAnterior > 0 ? "color:#c0392b;font-weight:600;"
                            : estado.SaldoAnterior < 0 ? "color:#1a8754;font-weight:600;" : "";
                html.AppendLine("<tr class='ant'>");
                html.AppendLine($"<td colspan='3' style='font-style:italic;color:#7a6200;font-size:8pt;'>Saldo anterior al período (movimientos anteriores a {DateTime.Now.AddMonths(-6):dd/MM/yyyy})</td>");
                html.AppendLine($"<td class='r'></td>");
                html.AppendLine($"<td class='r'></td>");
                html.AppendLine($"<td class='r' style='{saColor}'>{estado.SaldoAnterior.ToString("N2")}</td>");
                html.AppendLine("</tr>");
            }

            // Movimientos del período
            foreach (var m in estado.Movimientos)
            {
                var rowClass = m.Debe > 0 ? "debe" : m.Haber > 0 ? "haber" : "";
                var scColor  = m.Saldo > 0 ? "color:#c0392b;font-weight:600;"
                             : m.Saldo < 0 ? "color:#1a8754;font-weight:600;" : "";
                html.AppendLine($"<tr class='{rowClass}'>");
                html.AppendLine($"<td style='white-space:nowrap;font-size:8pt;'>{m.Fecha:dd/MM/yyyy}</td>");
                html.AppendLine($"<td>{HE(m.Movimiento)}</td>");
                html.AppendLine($"<td style='font-size:8pt;color:#6c757d;'>{HE(m.NumeroReferencia)}</td>");
                html.AppendLine($"<td class='r' style='color:#c0392b;'>{(m.Debe  > 0 ? m.Debe.ToString("N2")  : "")}</td>");
                html.AppendLine($"<td class='r' style='color:#1a8754;'>{(m.Haber > 0 ? m.Haber.ToString("N2") : "")}</td>");
                html.AppendLine($"<td class='r' style='{scColor}'>{m.Saldo.ToString("N2")}</td>");
                html.AppendLine("</tr>");
            }

            // Fila totales
            html.AppendLine("<tr class='tot'>");
            html.AppendLine("<td colspan='3' style='font-size:7.5pt;text-transform:uppercase;letter-spacing:.3px;color:#1a2a4a;'>Totales del período</td>");
            html.AppendLine($"<td class='r' style='color:#c0392b;'>{estado.TotalDebe.ToString("N2")}</td>");
            html.AppendLine($"<td class='r' style='color:#1a8754;'>{estado.TotalHaber.ToString("N2")}</td>");
            html.AppendLine("<td class='r'></td>");
            html.AppendLine("</tr>");
            html.AppendLine("</tbody></table></div>");

            // ── Saldo final ──────────────────────────────────────────────────
            var sfBg = sf > 0 ? "background:#fff0f0;border:1.5px solid #c0392b;"
                     : sf < 0 ? "background:#f0fff4;border:1.5px solid #1a8754;"
                     : "background:#f8f9fc;border:1.5px solid #dee2e6;";
            html.AppendLine($"<div class='saldo-box' style='{sfBg}'>");
            html.AppendLine($"<span style='font-size:8pt;font-weight:700;text-transform:uppercase;letter-spacing:.5px;color:#555;'>{sfLabel}</span>");
            html.AppendLine($"<span style='font-size:13pt;font-weight:700;color:{sfColor};'>{Math.Abs(sf).ToString("C2")}</span>");
            html.AppendLine("</div>");

            html.AppendLine("</div>"); // .page
            html.AppendLine("<button class='btn-print' onclick='window.print()'>🖨 Imprimir / Guardar PDF</button>");
            html.AppendLine("</body></html>");

            return Content(html.ToString(), "text/html");
        }

        /// <summary>Retorna el detalle de pago de un cobro para mostrar en la CC.</summary>
        public async Task<IActionResult> OnGetDetalleCobroAsync(int id)
        {
            var cobro = await _cobroService.GetCobroParaReciboAsync(id);
            if (cobro == null) return new JsonResult(new { ok = false });

            return new JsonResult(new
            {
                ok           = true,
                cobroId      = cobro.CobroId,
                fecha        = cobro.Fecha.ToString("dd/MM/yyyy"),
                importeTotal = cobro.ImporteTotal,
                detalle      = cobro.Detalle.Select(d => new
                {
                    medioPago = d.MedioPago,
                    importe   = d.Importe,
                    dato1     = d.Referencia1,
                    dato2     = d.Referencia2,
                    dato3     = d.Referencia3
                })
            });
        }

        // ----------------------------------------------------------------
        // COBRO
        // ----------------------------------------------------------------

        /// <summary>Retorna saldo pendiente del cliente y lista de planes de pago.</summary>
        public async Task<IActionResult> OnGetDatosCobro(int id)
        {
            // Saldo de la CC
            var movs = await _ccService.GetMovimientosAsync(id);
            var saldo = movs.Sum(m => m.Debe) - movs.Sum(m => m.Haber);
            var saldoACobrar = saldo > 0 ? saldo : 0m;

            // Planes con datos de su medio de pago
            var planes = await _cobroService.GetPlanesPagoConMedioAsync();

            return new JsonResult(new
            {
                saldo        = saldoACobrar,
                planes       = planes.Select(p => new
                {
                    planId     = p.PlanId,
                    planNombre = p.PlanNombre,
                    medioId    = p.MedioId,
                    medioNombre = p.MedioNombre,
                    conDatos   = p.ConDatos,
                    recargo    = p.Recargo
                })
            });
        }

        /// <summary>Verifica si la caja del usuario logueado está abierta.</summary>
        public async Task<IActionResult> OnGetVerificarCaja()
        {
            var haceCaja = await _cobroService.GetHaceCajaAsync();
            if (!haceCaja) return new JsonResult(new { ok = true, cajaId = 0 });

            var nombreUsuario = User.Identity?.Name ?? "";
            var userId = await _cobroService.GetUsuarioIdByNombreAsync(nombreUsuario);
            if (userId == 0) return new JsonResult(new { ok = false, msg = "Usuario no encontrado." });

            var (abierta, cajaId) = await _cobroService.VerificarCajaAsync(userId);
            if (!abierta) return new JsonResult(new { ok = false, msg = "Debe abrir la caja antes de registrar un cobro." });

            return new JsonResult(new { ok = true, cajaId });
        }

        /// <summary>Procesa el cobro. Recibe JSON con clienteId + importeTotal + detalle.</summary>
        public async Task<IActionResult> OnPostCobroAsync([FromBody] Domain.DTO.CobroRequestDto dto)
        {
            try
            {
                var haceCaja = await _cobroService.GetHaceCajaAsync();
                int cajaId = 0;

                if (haceCaja)
                {
                    var nombreUsuario = User.Identity?.Name ?? "";
                    var userId = await _cobroService.GetUsuarioIdByNombreAsync(nombreUsuario);
                    var (abierta, cId) = await _cobroService.VerificarCajaAsync(userId);
                    if (!abierta)
                        return new JsonResult(new { ok = false, msg = "Debe abrir la caja antes de registrar un cobro." });
                    cajaId = cId;
                }

                var cobroId = await _cobroService.RealizarCobroAsync(
                    dto.ClienteId, dto.ImporteTotal, haceCaja, cajaId, dto.Detalle);

                return new JsonResult(new { ok = true, cobroId });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, msg = ex.Message });
            }
        }

        /// <summary>Genera el recibo HTML del cobro para imprimir.</summary>
        public async Task<IActionResult> OnGetReciboCobroAsync(int id)
        {
            var cobro = await _cobroService.GetCobroParaReciboAsync(id);
            if (cobro == null) return NotFound();

            var empNombre = await _parametroService.ObtenerValorAsync("empresa", "nombre")      ?? "";
            var empRazon  = await _parametroService.ObtenerValorAsync("empresa", "razonSocial") ?? "";
            var empCuit   = await _parametroService.ObtenerValorAsync("empresa", "cuit")        ?? "";
            var empTel    = await _parametroService.ObtenerValorAsync("empresa", "telefono")    ?? "";
            var empEmail  = await _parametroService.ObtenerValorAsync("empresa", "email")       ?? "";

            string logoHtml = "";
            var logoParam = await _parametroService.GetLogoAsync();
            if (logoParam?.Imagen != null && logoParam.Imagen.Length > 0)
            {
                var img = logoParam.Imagen;
                string mime = "image/png";
                if (img.Length > 1)
                {
                    if      (img[0] == 0xFF && img[1] == 0xD8) mime = "image/jpeg";
                    else if (img[0] == 0x42 && img[1] == 0x4D) mime = "image/bmp";
                    else if (img[0] == 0x47 && img[1] == 0x49) mime = "image/gif";
                }
                logoHtml = $"<img src='data:{mime};base64,{Convert.ToBase64String(img)}' " +
                           "width='100' height='50' style='width:100px;height:50px;' />";
            }

            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html><html lang='es'><head><meta charset='UTF-8'>");
            html.AppendLine("<title>Recibo de Cobro</title>");
            html.AppendLine(@"<style>
*{box-sizing:border-box;margin:0;padding:0;}
body{font-family:Arial,Helvetica,sans-serif;font-size:9pt;color:#222;background:#fff;}
.page{max-width:780px;margin:0 auto;padding:8mm;}
.header-table{width:100%;border-collapse:collapse;border-bottom:2px solid #1a2a4a;margin-bottom:6mm;padding-bottom:4mm;}
.header-table td{vertical-align:middle;padding:3px 5px;}
.empresa-name{font-size:11pt;font-weight:bold;color:#1a2a4a;}
.empresa-sub{font-size:8pt;color:#555;line-height:1.5;}
.recibo-title{font-size:16pt;font-weight:bold;color:#1a2a4a;text-align:right;}
.recibo-num{font-size:9pt;color:#555;text-align:right;}
.section{margin-bottom:5mm;}
.section-title{font-size:7.5pt;font-weight:bold;text-transform:uppercase;letter-spacing:.5px;
               color:#fff;background:#1a2a4a;padding:3px 8px;margin-bottom:2mm;}
.info-table{width:100%;border-collapse:collapse;}
.info-table td{padding:2px 6px;font-size:8.5pt;border-bottom:1px solid #f0f0f0;}
.info-table td:first-child{font-weight:bold;color:#4e73df;width:130px;font-size:7.5pt;text-transform:uppercase;}
.data-table{width:100%;border-collapse:collapse;}
.data-table thead tr{background:#1a2a4a;}
.data-table th{padding:4px 6px;color:#fff;font-size:7pt;text-transform:uppercase;letter-spacing:.3px;}
.data-table td{padding:3px 6px;font-size:8.5pt;border-bottom:1px solid #eee;}
.data-table tr:nth-child(even) td{background:#f5f8ff;}
.total-box{margin-top:4mm;display:flex;justify-content:flex-end;}
.total-inner{background:#1a2a4a;color:#fff;padding:6px 16px;border-radius:6px;font-size:11pt;font-weight:bold;}
.saldo-box{display:flex;justify-content:flex-end;margin-top:2mm;}
.saldo-inner{font-size:8.5pt;color:#555;}
.foot{margin-top:8mm;font-size:7pt;color:#aaa;text-align:center;border-top:1px solid #eee;padding-top:3mm;}
@media print{body{background:white;}.no-print{display:none;}}
</style></head><body><div class='page'>");

            // ── Header
            html.AppendLine("<table class='header-table'><tr>");
            html.AppendLine($"  <td width='50%'>");
            if (!string.IsNullOrEmpty(logoHtml)) html.AppendLine($"    <div style='margin-bottom:4px;'>{logoHtml}</div>");
            html.AppendLine($"    <div class='empresa-name'>{HE(empNombre)}</div>");
            if (!string.IsNullOrEmpty(empRazon))  html.AppendLine($"    <div class='empresa-sub'>{HE(empRazon)}</div>");
            if (!string.IsNullOrEmpty(empCuit))   html.AppendLine($"    <div class='empresa-sub'>CUIT: {HE(empCuit)}</div>");
            if (!string.IsNullOrEmpty(empTel))    html.AppendLine($"    <div class='empresa-sub'>Tel: {HE(empTel)}</div>");
            if (!string.IsNullOrEmpty(empEmail))  html.AppendLine($"    <div class='empresa-sub'>Email: {HE(empEmail)}</div>");
            html.AppendLine("  </td>");
            html.AppendLine("  <td width='50%' style='text-align:right;'>");
            html.AppendLine("    <div class='recibo-title'>RECIBO DE COBRO</div>");
            html.AppendLine($"   <div class='recibo-num'>N° {cobro.CobroId:D8}</div>");
            html.AppendLine($"   <div class='recibo-num'>Fecha: {cobro.Fecha:dd/MM/yyyy HH:mm}</div>");
            html.AppendLine("  </td>");
            html.AppendLine("</tr></table>");

            // ── Datos del cliente
            html.AppendLine("<div class='section'>");
            html.AppendLine("<div class='section-title'>Datos del Cliente</div>");
            html.AppendLine("<table class='info-table'>");
            html.AppendLine($"<tr><td>Nombre Comercial</td><td>{HE(cobro.NombreComercial)}</td></tr>");
            html.AppendLine($"<tr><td>Razón Social</td><td>{HE(cobro.RazonSocial)}</td></tr>");
            var partesDir = new[] { cobro.Direccion, cobro.LocalidadDescripcion, cobro.ProvinciaDescripcion }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            var dirCompleta = string.Join(", ", partesDir);
            if (!string.IsNullOrWhiteSpace(dirCompleta))
                html.AppendLine($"<tr><td>Dirección</td><td>{HE(dirCompleta)}</td></tr>");
            html.AppendLine($"<tr><td>Teléfono</td><td>{HE(cobro.Telefono)}</td></tr>");
            html.AppendLine($"<tr><td>CUIL/CUIT</td><td>{HE(cobro.Cuil)}</td></tr>");
            html.AppendLine("</table></div>");

            // ── Detalle del cobro
            html.AppendLine("<div class='section'>");
            html.AppendLine("<div class='section-title'>Detalle del Cobro</div>");
            html.AppendLine("<table class='data-table'><thead><tr>");
            html.AppendLine("<th>Medio de Pago</th><th style='text-align:right;'>Importe</th>");
            html.AppendLine("<th>Dato 1</th><th>Dato 2</th><th>Dato 3</th>");
            html.AppendLine("</tr></thead><tbody>");
            foreach (var d in cobro.Detalle)
            {
                html.AppendLine($"<tr><td>{HE(d.MedioPago)}</td>");
                html.AppendLine($"<td style='text-align:right;font-weight:bold;'>{d.Importe.ToString("N2")}</td>");
                html.AppendLine($"<td style='text-align:right;'>{HE(d.Referencia1)}</td><td style='text-align:right;'>{HE(d.Referencia2)}</td><td style='text-align:right;'>{HE(d.Referencia3)}</td></tr>");
            }
            html.AppendLine("</tbody></table>");

            // Total + saldo post-cobro
            html.AppendLine($"<div class='total-box'><div class='total-inner'>Total Cobrado: {cobro.ImporteTotal.ToString("C2")}</div></div>");
            if (cobro.SaldoPostCobro > 0)
                html.AppendLine($"<div class='saldo-box'><div class='saldo-inner'>Saldo pendiente post-cobro: <strong style='color:#e74a3b;'>{cobro.SaldoPostCobro.ToString("C2")}</strong></div></div>");
            else if (cobro.SaldoPostCobro <= 0)
                html.AppendLine($"<div class='saldo-box'><div class='saldo-inner' style='color:#1cc88a;font-weight:bold;'>&#10003; Cuenta cancelada</div></div>");
            html.AppendLine("</div>");

            html.AppendLine($"<div class='foot'>Recibo N° {cobro.CobroId:D8} &mdash; {HE(empNombre)} &mdash; {cobro.Fecha:dd/MM/yyyy}</div>");
            html.AppendLine("</div>");
            html.AppendLine(@"<script>
window.onload = function(){
    setTimeout(function(){
        var btn = document.createElement('div');
        btn.className='no-print';
        btn.style='text-align:center;margin:10px;';
        btn.innerHTML='<button onclick=""window.print()"" style=""padding:8px 24px;background:#1a2a4a;color:#fff;border:none;border-radius:6px;font-size:10pt;cursor:pointer;"">🖨 Imprimir</button>';
        document.body.insertBefore(btn, document.body.firstChild);
    }, 100);
};
</script>");
            html.AppendLine("</body></html>");

            return Content(html.ToString(), "text/html; charset=utf-8");
        }

        // ----------------------------------------------------------------
        // NOTA DE CRÉDITO
        // ----------------------------------------------------------------

        /// <summary>Datos iniciales para el modal NC (IVA, IIBB, si tiene FE).</summary>
        public async Task<IActionResult> OnGetDatosNCAsync()
        {
            var datos = await _ncService.GetDatosInicialesAsync();
            return new JsonResult(new
            {
                facturaElectronica = datos.FacturaElectronica,
                ivas      = datos.Ivas.Select(i => new { i.Id, i.Valor }),
                impuestos = datos.Impuestos.Select(i => new { i.Id, i.Valor })
            });
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostNotaDebitoAsync([FromBody] Domain.DTO.NotaDebitoRequestDto dto)
        {
            if (dto.ClienteId <= 0)
                return new JsonResult(new { ok = false, msg = "Cliente inválido." });
            if (dto.Importe <= 0)
                return new JsonResult(new { ok = false, msg = "El importe debe ser mayor a 0." });
            if (string.IsNullOrWhiteSpace(dto.Observaciones))
                return new JsonResult(new { ok = false, msg = "Las observaciones son obligatorias." });

            try
            {
                var ndId = await _ncService.ProcesarNDAsync(dto.ClienteId, dto.Importe, dto.Observaciones.Trim().ToUpperInvariant());
                return new JsonResult(new { ok = true, ndId });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, msg = "Error al procesar: " + ex.Message });
            }
        }

        /// <summary>Procesa la Nota de Crédito.</summary>
        public async Task<IActionResult> OnPostNotaCreditoAsync([FromBody] Domain.DTO.NotaCreditoRequestDto dto)
        {
            try
            {
                if (dto.Importe <= 0)
                    return new JsonResult(new { ok = false, msg = "El importe debe ser mayor a 0." });
                if (string.IsNullOrWhiteSpace(dto.Observaciones))
                    return new JsonResult(new { ok = false, msg = "Las observaciones son obligatorias." });
                if (dto.FacturaAsociada.HasValue && dto.FacturaAsociada <= 0)
                    return new JsonResult(new { ok = false, msg = "La factura asociada debe ser un número válido." });

                var (cobroId, error) = await _ncService.ProcesarNCAsync(dto);
                if (error != null)
                    return new JsonResult(new { ok = false, msg = error });

                return new JsonResult(new { ok = true, cobroId });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, msg = ex.Message });
            }
        }

        // ----------------------------------------------------------------
        // HELPERS
        // ----------------------------------------------------------------
        private static string HE(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");

        private static string DetField(string label, string value) =>
            $"<div style=\"display:flex;flex-direction:column;padding:.5rem .75rem;border-bottom:1px solid #f0f0f0;\">" +
            $"<span style=\"font-size:.7rem;font-weight:700;text-transform:uppercase;letter-spacing:.5px;color:#4e73df;margin-bottom:2px;\">{label}</span>" +
            $"<span style=\"font-size:.83rem;color:#2d3748;\">{(string.IsNullOrWhiteSpace(value) || value == "&#xA;&#xA;" ? "<span style='color:#adb5bd;font-style:italic;'>—</span>" : value)}</span>" +
            "</div>";

        private static string DetFieldRow(string lbl1, string val1, string lbl2, string val2) =>
            "<div style=\"display:flex;flex-direction:row;gap:2rem;padding:.5rem .75rem;border-bottom:1px solid #f0f0f0;\">" +
            $"<div style=\"flex:1;\"><span style=\"font-size:.7rem;font-weight:700;text-transform:uppercase;letter-spacing:.5px;color:#4e73df;display:block;margin-bottom:2px;\">{lbl1}</span>" +
            $"<span style=\"font-size:.83rem;color:#2d3748;\">{(string.IsNullOrWhiteSpace(val1) ? "<span style='color:#adb5bd;font-style:italic;'>—</span>" : val1)}</span></div>" +
            $"<div style=\"flex:1;\"><span style=\"font-size:.7rem;font-weight:700;text-transform:uppercase;letter-spacing:.5px;color:#4e73df;display:block;margin-bottom:2px;\">{lbl2}</span>" +
            $"<span style=\"font-size:.83rem;color:#2d3748;\">{(string.IsNullOrWhiteSpace(val2) ? "<span style='color:#adb5bd;font-style:italic;'>—</span>" : val2)}</span></div>" +
            "</div>";
    }
}
