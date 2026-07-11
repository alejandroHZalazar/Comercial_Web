using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using System.Text.Json;
using Application.Interfaces;

namespace Comercial_Web.Pages.Ventas.Ventas;

// ── DTO para guardar componentes de promo desde JS ───────────────────────────
public class GuardarComponentesVentaDto
{
    public long                    VentaId { get; set; }
    public List<PromoGrupoDto>     Grupos  { get; set; } = new();
}

public class PromoGrupoDto
{
    public int                      FkProductoPromo { get; set; }
    public List<PromoComponenteDto> Componentes     { get; set; } = new();
}

[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly IVentaService          _ventaService;
    private readonly IPedidoService         _pedidoService;
    private readonly IUsuarioService        _usuarioService;
    private readonly IClienteService        _clienteService;
    private readonly ICondicionIvaService   _condIvaService;
    private readonly ILocalidadService      _localidadService;
    private readonly IZonaClienteService    _zonaService;
    private readonly ICobroService          _cobroService;
    private readonly IPorcentajeIvaService  _porcIvaService;
    private readonly IPromocionService      _promoService;
    protected readonly IParametroService    _parametroService;

    public IndexModel(
        IVentaService         ventaService,
        IPedidoService        pedidoService,
        IUsuarioService       usuarioService,
        IClienteService       clienteService,
        ICondicionIvaService  condIvaService,
        ILocalidadService     localidadService,
        IZonaClienteService   zonaService,
        ICobroService         cobroService,
        IPorcentajeIvaService porcIvaService,
        IPromocionService     promoService,
        IParametroService     parametroService)
    {
        _ventaService     = ventaService;
        _pedidoService    = pedidoService;
        _usuarioService   = usuarioService;
        _clienteService   = clienteService;
        _condIvaService   = condIvaService;
        _localidadService = localidadService;
        _zonaService      = zonaService;
        _cobroService     = cobroService;
        _porcIvaService   = porcIvaService;
        _promoService     = promoService;
        _parametroService = parametroService;
    }

    // ── Datos para la vista ─────────────────────────────────────────────
    public VentaParametrosDto        Params           { get; private set; } = new();
    public List<SelectListItem>      ListaVendedores  { get; private set; } = new();
    public List<SelectListItem>      ListaImpuestos   { get; private set; } = new();
    public List<SelectListItem>      ListaIvas        { get; private set; } = new();
    public List<SelectListItem>      ListaProveedores { get; private set; } = new();
    public List<SelectListItem>      ListaCondIvas    { get; private set; } = new();
    public List<SelectListItem>      ListaLocalidades { get; private set; } = new();
    public List<SelectListItem>      ListaZonas       { get; private set; } = new();
    public List<PlanPagoConMedioDto> PlanesPago       { get; private set; } = new();
    public int                       UsuarioId        { get; private set; }

    protected virtual bool OmitirRedirectMinorista => false;

    // ── OnGet ────────────────────────────────────────────────────────────
    public virtual async Task<IActionResult> OnGetAsync()
    {
        if (!OmitirRedirectMinorista)
        {
            var esMin = await _parametroService.ObtenerValorAsync("empresa", "esMinorista");
            if (esMin == "1")
                return RedirectToPage("/Ventas/VentasMinorista/Index");
        }

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

        PlanesPago = await _cobroService.GetPlanesPagoConMedioAsync();

        var condIvas = await _condIvaService.GetAllAsync();
        ListaCondIvas = condIvas
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Descripcion })
            .ToList();

        var localidades = await _localidadService.GetAllAsync();
        ListaLocalidades = localidades
            .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Nombre ?? "" })
            .ToList();

        var zonas = await _zonaService.GetAllAsync();
        ListaZonas = zonas
            .Where(z => z.Baja != true)
            .Select(z => new SelectListItem { Value = z.Id.ToString(), Text = z.Nombre ?? "" })
            .ToList();

        var nombre = User.FindFirstValue(ClaimTypes.Name) ?? "";
        UsuarioId = await _cobroService.GetUsuarioIdByNombreAsync(nombre);
        return Page();
    }

    // ── Helper para el multi-select de clientes ──────────────────────────
    public async Task<List<SelectListItem>> GetClientesListAsync()
    {
        var todos = await _clienteService.GetAllAsync();
        return todos
            .Where(c => c.Baja != true)
            .OrderBy(c => c.NombreComercial)
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.NombreComercial ?? "" })
            .ToList();
    }

    // ── Handlers GET ─────────────────────────────────────────────────────

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

        // Recargar parámetros (el handler no ejecuta OnGetAsync)
        Params = await _ventaService.GetParametrosAsync(Environment.MachineName);

        List<int>? listaProvs = null;
        if (Params.FiltraPorProveedor == 1 && !string.IsNullOrWhiteSpace(proveedores))
        {
            listaProvs = proveedores.Split(',')
                .Select(v => int.TryParse(v.Trim(), out var x) ? x : 0)
                .Where(x => x > 0)
                .ToList();
        }

        var lista = await _pedidoService.BuscarProductosAsync(q, tipo,
            Params.DolarizaProductos == 1, Params.ValorDolar, listaProvs);
        return new JsonResult(lista);
    }

    public async Task<IActionResult> OnGetPreciosActualesAsync(string ids)
    {
        if (string.IsNullOrWhiteSpace(ids)) return new JsonResult(new List<object>());
        var listaIds = ids.Split(',')
            .Select(v => int.TryParse(v.Trim(), out var x) ? x : 0)
            .Where(x => x > 0).ToList();
        var precios = await _ventaService.GetPreciosActualesAsync(
            listaIds, Params.DolarizaProductos == 1, Params.ValorDolar);
        return new JsonResult(precios);
    }

    public async Task<IActionResult> OnGetBuscarPedidosAsync(
        string? desde, string? hasta, string? vendedores, string? clientes)
    {
        DateTime? dDesde = null, dHasta = null;
        if (DateTime.TryParse(desde, out var d)) dDesde = d;
        if (DateTime.TryParse(hasta, out var h)) dHasta = h;

        var listVend = string.IsNullOrWhiteSpace(vendedores) ? new List<int>()
            : vendedores.Split(',').Select(v => int.TryParse(v.Trim(), out var x) ? x : 0).Where(x => x > 0).ToList();
        var listCli = string.IsNullOrWhiteSpace(clientes) ? new List<int>()
            : clientes.Split(',').Select(v => int.TryParse(v.Trim(), out var x) ? x : 0).Where(x => x > 0).ToList();

        var lista = await _pedidoService.BuscarPedidosAsync(dDesde, dHasta, listVend, listCli);
        return new JsonResult(lista);
    }

    public async Task<IActionResult> OnGetPedidoAsync(int id)
    {
        var cab = await _pedidoService.GetCabeceraAsync(id);
        if (cab == null) return new JsonResult(new { ok = false });
        var det = await _pedidoService.GetDetalleAsync(id);
        return new JsonResult(new {
            ok = true,
            cab = new {
                id              = cab.Id,
                fkCliente       = cab.FkCliente,
                iva             = cab.Iva,
                descuento       = cab.Descuento,
                recargo         = cab.Recargo,
                nombreComercial = cab.NombreComercial,
                fecha           = cab.Fecha,
                observacion     = cab.Observacion
            },
            detalle = det.Select(d => new {
                fkProducto    = d.FkProducto,
                codBarras     = d.CodBarras,
                codProveedor  = d.CodProveedor,
                descripcion   = d.Descripcion,
                precioSinIva  = d.PrecioSinIva,
                precioConIva  = d.PrecioConIva,
                cantidad      = d.Cantidad,
                descuento     = d.Descuento,
                recargo       = d.Recargo,
                costo         = d.Costo,
                fraccionado   = d.Fraccionado,
                subtotal      = d.Subtotal
            })
        });
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

    public async Task<IActionResult> OnGetCajaAsync()
    {
        var nombre  = User.FindFirstValue(ClaimTypes.Name) ?? "";
        int usuId   = await _cobroService.GetUsuarioIdByNombreAsync(nombre);
        var (abierta, cajaId) = await _ventaService.GetCajaAbiertaAsync(usuId);
        return new JsonResult(new { abierta, cajaId });
    }

    // ── Handlers POST ────────────────────────────────────────────────────

    public async Task<IActionResult> OnPostCrearClienteRapidoAsync([FromBody] CrearClienteRapidoDto? dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.NombreComercial))
            return new JsonResult(new { ok = false, msg = "Nombre requerido." });
        try
        {
            var (id, nombre, tel, contacto, dir) = await _pedidoService.CrearClienteRapidoAsync(dto);
            return new JsonResult(new { ok = true, clienteId = id, nombreComercial = nombre, telefono = tel, contacto, direccion = dir });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { ok = false, msg = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostGrabarVentaAsync([FromBody] GrabarVentaRequestDto? dto)
    {
        if (dto == null || dto.FkCliente <= 0 || dto.Detalle.Count == 0)
            return new JsonResult(new GrabarVentaResultDto { Ok = false, Msg = "Datos inválidos: verifique cliente y detalle." });
        try
        {
            var nombre = User.FindFirstValue(ClaimTypes.Name) ?? "";
            dto.FkCajero = await _cobroService.GetUsuarioIdByNombreAsync(nombre);

            if (dto.HaceCaja && dto.CajaId == 0)
            {
                var (abierta, cajaId) = await _ventaService.GetCajaAbiertaAsync(dto.FkCajero);
                dto.CajaId = abierta ? cajaId : 0;
            }

            var ventaId = await _ventaService.GrabarVentaAsync(dto);
            return new JsonResult(new GrabarVentaResultDto { Ok = true, VentaId = ventaId });
        }
        catch (Exception ex)
        {
            return new JsonResult(new GrabarVentaResultDto { Ok = false, Msg = ex.Message });
        }
    }

    // ── Promociones ──────────────────────────────────────────────────────────

    // Calcula qué promos se pueden formar con los productos del carrito
    public async Task<IActionResult> OnPostCalcularPromocionesAsync(
        [FromBody] List<ItemCarritoDto>? carrito)
    {
        var activo = await _parametroService.ObtenerValorAsync("promociones", "activo");
        if (activo != "1" || carrito == null || carrito.Count == 0)
            return new JsonResult(
                new { hayPromociones = false, formadas = Array.Empty<object>(), casiCompletas = Array.Empty<object>() });

        var result = await _promoService.CalcularPromocionesAsync(carrito);
        return new JsonResult(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    // Guarda los componentes usados en las promos y descuenta stock (llamado post-venta)
    public async Task<IActionResult> OnPostGuardarComponentesAsync(
        [FromBody] GuardarComponentesVentaDto? dto)
    {
        if (dto == null || dto.VentaId <= 0)
            return new JsonResult(new { ok = false, msg = "Datos inválidos." });

        foreach (var grupo in dto.Grupos)
        {
            await _promoService.GuardarComponentesPorVentaAsync(
                dto.VentaId, grupo.FkProductoPromo, grupo.Componentes);
        }

        return new JsonResult(new { ok = true });
    }

    public async Task<IActionResult> OnPostFacturarAsync([FromBody] FacturarDto? dto)
    {
        if (dto == null || dto.VentaId <= 0)
            return new JsonResult(new { ok = false, msg = "VentaId inválido." });
        try
        {
            var result = await _ventaService.EmitirFacturaElectronicaAsync(dto.VentaId);
            return new JsonResult(new
            {
                ok             = result.Ok,
                cae            = result.Cae,
                vencimientoCae = result.VencimientoCae,
                nroComprobante = result.NumeroComprobante,
                pdfUrl         = result.PdfUrl,
                errores        = result.Errores
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { ok = false, msg = ex.Message });
        }
    }
}

public class FacturarDto
{
    public long VentaId { get; set; }
}
