using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Comercial_Web.Pages.Ventas.Devolucion;

[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly IVentaService                  _ventaService;
    private readonly IPedidoService                 _pedidoService;
    private readonly IUsuarioService                _usuarioService;
    private readonly IPorcentajeIvaService          _porcIvaService;
    private readonly IReporteVentasService          _reporteService;
    private readonly IDevolucionService             _devolucionService;
    private readonly IFacturacionElectronicaService _feService;

    public IndexModel(
        IVentaService                  ventaService,
        IPedidoService                 pedidoService,
        IUsuarioService                usuarioService,
        IPorcentajeIvaService          porcIvaService,
        IReporteVentasService          reporteService,
        IDevolucionService             devolucionService,
        IFacturacionElectronicaService feService)
    {
        _ventaService      = ventaService;
        _pedidoService     = pedidoService;
        _usuarioService    = usuarioService;
        _porcIvaService    = porcIvaService;
        _reporteService    = reporteService;
        _devolucionService = devolucionService;
        _feService         = feService;
    }

    public VentaParametrosDto   Params           { get; private set; } = new();
    public List<SelectListItem> ListaVendedores  { get; private set; } = new();
    public List<SelectListItem> ListaImpuestos   { get; private set; } = new();
    public List<SelectListItem> ListaIvas        { get; private set; } = new();
    public List<SelectListItem> ListaProveedores { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Params = await _ventaService.GetParametrosAsync(Environment.MachineName);

        var vendedores = await _usuarioService.GetAllAsync();
        ListaVendedores = vendedores
            .Where(u => u.Baja != true)
            .OrderBy(u => u.Nombre)
            .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Nombre ?? "" })
            .ToList();

        var impuestos = await _ventaService.GetImpuestosAsync();
        ListaImpuestos = impuestos
            .Select(i => new SelectListItem
            {
                Value = (i.Valor ?? 0m).ToString(System.Globalization.CultureInfo.InvariantCulture),
                Text  = (i.Valor ?? 0m).ToString("N2", new System.Globalization.CultureInfo("es-AR")) + "%"
            })
            .ToList();

        var ivas = await _porcIvaService.GetAllAsync();
        ListaIvas = ivas
            .OrderBy(i => i.Valor ?? 0m)
            .Select(i => new SelectListItem
            {
                Value = (i.Valor ?? 0m).ToString(System.Globalization.CultureInfo.InvariantCulture),
                Text  = (i.Valor ?? 0m).ToString("N2", new System.Globalization.CultureInfo("es-AR")) + "%"
            })
            .ToList();

        if (Params.FiltraPorProveedor == 1)
        {
            var provs = await _ventaService.GetProveedoresActivosAsync();
            ListaProveedores = provs
                .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.NombreComercial ?? "" })
                .ToList();
        }
    }

    // ── Handlers de datos ────────────────────────────────────────────────────

    public async Task<IActionResult> OnGetBuscarClientesAsync(string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return new JsonResult(new List<object>());
        return new JsonResult(await _pedidoService.BuscarClientesAsync(q));
    }

    public async Task<IActionResult> OnGetClienteDataAsync(int id)
    {
        var data = await _ventaService.GetClienteDataAsync(id);
        if (data == null) return new JsonResult(new { ok = false });
        return new JsonResult(new { ok = true, data });
    }

    public async Task<IActionResult> OnGetBuscarProductosAsync(
        string q, string tipo, string? proveedores)
    {
        if (string.IsNullOrWhiteSpace(q)) return new JsonResult(new List<object>());

        Params = await _ventaService.GetParametrosAsync(Environment.MachineName);

        List<int>? listaProvs = null;
        if (Params.FiltraPorProveedor == 1 && !string.IsNullOrWhiteSpace(proveedores))
        {
            listaProvs = proveedores.Split(',')
                .Select(v => int.TryParse(v.Trim(), out var x) ? x : 0)
                .Where(x => x > 0).ToList();
        }

        var lista = await _pedidoService.BuscarProductosAsync(q, tipo,
            Params.DolarizaProductos == 1, Params.ValorDolar, listaProvs);
        return new JsonResult(lista);
    }

    public async Task<IActionResult> OnGetStockProductosAsync(string ids)
    {
        if (string.IsNullOrWhiteSpace(ids)) return new JsonResult(new object());
        var listaIds = ids.Split(',')
            .Select(v => int.TryParse(v.Trim(), out var x) ? x : 0)
            .Where(x => x > 0).ToList();
        var stocks = await _ventaService.GetStockProductosAsync(listaIds);
        return new JsonResult(stocks);
    }

    public async Task<IActionResult> OnGetBuscarVentasAsync(
        string? desde, string? hasta, string? clientes)
    {
        DateTime? dDesde = null, dHasta = null;
        if (DateTime.TryParse(desde, out var d)) dDesde = d;
        if (DateTime.TryParse(hasta, out var h)) dHasta = h;

        var listCli = string.IsNullOrWhiteSpace(clientes) ? new List<int>()
            : clientes.Split(',')
                .Select(v => int.TryParse(v.Trim(), out var x) ? x : 0)
                .Where(x => x > 0).ToList();

        var lista = await _reporteService.BuscarVentasAsync(dDesde, dHasta, listCli);
        return new JsonResult(lista);
    }

    public async Task<IActionResult> OnGetVentaDetalleAsync(long id)
    {
        var dto = await _ventaService.GetVentaParaImpresionAsync(id);
        if (dto == null) return new JsonResult(new { ok = false });
        return new JsonResult(new { ok = true, venta = dto });
    }

    // ── Handler principal: guardar devolución ────────────────────────────────

    public async Task<IActionResult> OnPostDevolverAsync([FromBody] DevolverWebRequestDto request)
    {
        try
        {
            if (request.FkCliente <= 0)
                return new JsonResult(new { ok = false, error = "Debe seleccionar un cliente." });
            if (!request.Filas.Any())
                return new JsonResult(new { ok = false, error = "Debe agregar al menos un producto." });

            // Cajero = usuario logueado
            var usuarios = await _usuarioService.GetAllAsync();
            var cajero   = usuarios.FirstOrDefault(u => u.Nombre == User.Identity!.Name);
            int fkCajero = cajero?.Id ?? 0;

            // Parámetros del sistema
            var prm = await _ventaService.GetParametrosAsync(Environment.MachineName);

            // Desc/Rec de cabecera:
            //  - Modo por línea (BonificacionPorLinea==1): descuento GENERAL sobre Total S/IVA
            //    (heredado/editable de la venta), enviado en request.Descuento. Recargo = 0.
            //  - Modo global: si todas las filas comparten el mismo desc/rec → se envía como global.
            decimal descuento = 0m, recargo = 0m;
            if (prm.BonificacionPorLinea == 1)
            {
                descuento = request.Descuento ?? 0m;
            }
            else if (request.Filas.Count > 0)
            {
                var dr0     = request.Filas[0].DescRec;
                bool mismo  = request.Filas.All(f => f.DescRec == dr0);
                if (mismo && dr0 != 0)
                {
                    if (dr0 < 0) descuento = Math.Abs(dr0);
                    else         recargo   = dr0;
                }
            }

            // Costo total (calculado en service desde costosProductos, pero lo enviamos como referencia)
            decimal costo = request.Filas
                .Sum(f => Math.Round(f.Costo * f.Cantidad, 4));

            var dto = new DevolucionRequestDto
            {
                Total      = request.Total,
                Costo      = costo,
                FkCliente  = request.FkCliente,
                FkCajero   = fkCajero,
                Iva        = request.Iva,
                Descuento  = descuento,
                Recargo    = recargo,
                FkVendedor = request.FkVendedor,
                Comision   = request.Comision,
                Impuesto   = request.Impuesto,
                LlevaCC    = prm.LlevaCC,
                Filas      = request.Filas
            };

            long devolucionId = await _devolucionService.GrabarDevolucionAsync(dto);
            if (devolucionId == -1)
                return new JsonResult(new { ok = false, error = "Error al guardar la devolución en la base de datos." });

            // Facturación electrónica: emitir NC si se indicó comprobante asociado
            // (si la devolución supera 130 ítems, EmitirNotaCreditoManualAsync devuelve
            // varios comprobantes; se exponen todos en "comprobantes" para que la UI
            // no muestre solo el último PDF/CAE).
            string? warningFE = null;
            string? pdfUrlFE  = null;
            List<ComprobanteEmitidoResultItemDto>? comprobantesFE = null;
            if (prm.FacturaElectronica == 1
                && request.NroFacturaAsociada.HasValue
                && request.NroFacturaAsociada.Value > 0)
            {
                var ncDto = new NotaCreditoRequestDto
                {
                    ClienteId        = request.FkCliente,
                    Importe          = request.Total,
                    FacturaAsociada  = request.NroFacturaAsociada,
                    FechaFacturaAsoc = request.FechaFacturaAsoc,
                    IvaPorcentaje    = request.Iva,
                    IIBBPorcentaje   = request.Impuesto,
                    Observaciones    = $"Devolución N° {devolucionId}",
                    IdDevolucion     = (int)devolucionId
                };

                var feResult = await _feService.EmitirNotaCreditoManualAsync(ncDto, prm.PuntoVenta);
                comprobantesFE = feResult.Comprobantes;
                if (feResult.Ok)
                    pdfUrlFE = feResult.PdfUrl;
                else
                    warningFE = $"Devolución guardada, pero ocurrió un error al emitir la Nota de Crédito fiscal: {feResult.Error}";
            }

            return new JsonResult(new
            {
                ok = true, devolucionId, warning = warningFE, pdfUrl = pdfUrlFE,
                comprobantes = comprobantesFE
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { ok = false, error = ex.Message });
        }
    }

}
