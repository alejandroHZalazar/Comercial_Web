using Application.Interfaces;
using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Comercial_Web.Pages.Ventas.Pedidos;

[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly IPedidoService        _pedidoService;
    private readonly IPorcentajeIvaService _ivaService;
    private readonly IUsuarioService       _usuarioService;
    private readonly IParametroService     _parametroService;
    private readonly ICondicionIvaService  _condIvaService;
    private readonly ILocalidadService     _localidadService;
    private readonly IZonaClienteService   _zonaClienteService;
    private readonly IClienteService       _clienteService;

    public IndexModel(
        IPedidoService        pedidoService,
        IPorcentajeIvaService ivaService,
        IUsuarioService       usuarioService,
        IParametroService     parametroService,
        ICondicionIvaService  condIvaService,
        ILocalidadService     localidadService,
        IZonaClienteService   zonaClienteService,
        IClienteService       clienteService)
    {
        _pedidoService      = pedidoService;
        _ivaService         = ivaService;
        _usuarioService     = usuarioService;
        _parametroService   = parametroService;
        _condIvaService     = condIvaService;
        _localidadService   = localidadService;
        _zonaClienteService = zonaClienteService;
        _clienteService     = clienteService;
    }

    public List<SelectListItem> ListaIvas        { get; private set; } = new();
    public List<SelectListItem> ListaVendedores  { get; private set; } = new();
    public List<SelectListItem> ListaCondIvas    { get; private set; } = new();
    public List<SelectListItem> ListaLocalidades { get; private set; } = new();
    public List<SelectListItem> ListaZonas       { get; private set; } = new();

    // Parámetros del sistema (se serializan a JS)
    public int     BonificacionPorLinea  { get; private set; }
    public int     ProductosDolarizados  { get; private set; }
    public decimal CotizDolar            { get; private set; }

    public async Task OnGetAsync()
    {
        var ivas = await _ivaService.GetAllAsync();
        ListaIvas = ivas
            .OrderBy(i => i.Valor)
            .Select(i => new SelectListItem
            {
                Value = (i.Valor ?? 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
                Text  = (i.Valor ?? 0).ToString(System.Globalization.CultureInfo.InvariantCulture) + "%"
            }).ToList();

        var vendedores = await _usuarioService.GetAllAsync();
        ListaVendedores = vendedores
            .Where(u => u.Baja != true)
            .OrderBy(u => u.Nombre)
            .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Nombre ?? "" })
            .ToList();

        var condIvas = await _condIvaService.GetAllAsync();
        ListaCondIvas = condIvas
            .OrderBy(c => c.Descripcion)
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Descripcion ?? "" })
            .ToList();

        var localidades = await _localidadService.GetAllAsync();
        ListaLocalidades = localidades
            .Where(l => l.Baja != true)
            .OrderBy(l => l.Nombre)
            .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Nombre ?? "" })
            .ToList();

        var zonas = await _zonaClienteService.GetAllAsync();
        ListaZonas = zonas
            .Where(z => z.Baja != true)
            .OrderBy(z => z.Nombre)
            .Select(z => new SelectListItem { Value = z.Id.ToString(), Text = z.Nombre ?? "" })
            .ToList();

        var bonStr    = await _parametroService.ObtenerValorAsync("ventas",    "bonificacionesPorDetalle");
        var dolStr    = await _parametroService.ObtenerValorAsync("productos", "dolarizaProductos");
        var cotizStr  = await _parametroService.ObtenerValorAsync("productos", "cotizacionDolar");

        BonificacionPorLinea = string.IsNullOrWhiteSpace(bonStr)   ? 0 : (int.TryParse(bonStr,   out var b) ? b : 0);
        ProductosDolarizados = string.IsNullOrWhiteSpace(dolStr)    ? 0 : (int.TryParse(dolStr,   out var d) ? d : 0);
        CotizDolar           = string.IsNullOrWhiteSpace(cotizStr)  ? 0m: (decimal.TryParse(cotizStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var c) ? c : 0m);
    }

    // ── Lista de clientes para multi-select ────────────────────────────────
    public async Task<List<SelectListItem>> GetClientesListAsync()
    {
        var todos = await _clienteService.GetAllAsync();
        return todos
            .Where(c => c.Baja != true)
            .OrderBy(c => c.NombreComercial)
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.NombreComercial ?? "" })
            .ToList();
    }

    // ── AJAX: autocomplete clientes ──────────────────────────────────────
    public async Task<IActionResult> OnGetBuscarClientesAsync(string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return new JsonResult(new List<object>());
        var lista = await _pedidoService.BuscarClientesAsync(q);
        return new JsonResult(lista);
    }

    // ── AJAX: autocomplete productos ─────────────────────────────────────
    public async Task<IActionResult> OnGetBuscarProductosAsync(string q, string tipo)
    {
        if (string.IsNullOrWhiteSpace(q)) return new JsonResult(new List<object>());
        var dolStr   = await _parametroService.ObtenerValorAsync("productos", "dolarizaProductos");
        var cotizStr = await _parametroService.ObtenerValorAsync("productos", "cotizacionDolar");
        bool dol  = dolStr == "1";
        decimal.TryParse(cotizStr, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var cotiz);
        var lista = await _pedidoService.BuscarProductosAsync(q, tipo ?? "descripcion", dol, cotiz);
        return new JsonResult(lista);
    }

    // ── AJAX: buscar pedidos existentes ──────────────────────────────────
    public async Task<IActionResult> OnGetBuscarPedidosAsync(
        string? desde, string? hasta, string? vendedores, string? clientes)
    {
        DateTime? dDesde = null, dHasta = null;
        if (DateTime.TryParse(desde, out var d))  dDesde = d;
        if (DateTime.TryParse(hasta, out var h))  dHasta = h;

        var listVend = string.IsNullOrWhiteSpace(vendedores) ? new List<int>()
            : vendedores.Split(',').Select(v => int.TryParse(v.Trim(), out var x) ? x : 0).Where(x => x > 0).ToList();
        var listCli  = string.IsNullOrWhiteSpace(clientes)  ? new List<int>()
            : clientes.Split(',').Select(v => int.TryParse(v.Trim(), out var x) ? x : 0).Where(x => x > 0).ToList();

        var lista = await _pedidoService.BuscarPedidosAsync(dDesde, dHasta, listVend, listCli);
        return new JsonResult(lista);
    }

    // ── AJAX: obtener pedido completo para editar ────────────────────────
    public async Task<IActionResult> OnGetPedidoAsync(int id)
    {
        var cab     = await _pedidoService.GetCabeceraAsync(id);
        if (cab == null) return new JsonResult(new { ok = false });
        var detalle = await _pedidoService.GetDetalleAsync(id);
        return new JsonResult(new { ok = true, cab, detalle });
    }

    // ── POST: crear cliente rápido ────────────────────────────────────────
    public async Task<IActionResult> OnPostCrearClienteRapidoAsync([FromBody] CrearClienteRapidoDto? dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.NombreComercial))
            return new JsonResult(new { ok = false, msg = "El nombre comercial es obligatorio." });
        try
        {
            var (id, nombre, tel, contacto, dir) = await _pedidoService.CrearClienteRapidoAsync(dto);
            return new JsonResult(new { ok = true, clienteId = id, nombreComercial = nombre,
                telefono = tel, contacto, direccion = dir });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { ok = false, msg = ex.Message });
        }
    }

    // ── POST: guardar pedido ─────────────────────────────────────────────
    public async Task<IActionResult> OnPostGuardarPedidoAsync([FromBody] GuardarPedidoRequestDto dto)
    {
        if (dto.FkCliente <= 0)
            return new JsonResult(new { ok = false, msg = "Debe seleccionar un cliente." });
        if (dto.Detalle == null || dto.Detalle.Count == 0)
            return new JsonResult(new { ok = false, msg = "Debe agregar al menos un producto." });
        try
        {
            var pedidoId = await _pedidoService.GuardarPedidoAsync(dto);
            return new JsonResult(new { ok = true, pedidoId });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { ok = false, msg = "Error: " + ex.Message });
        }
    }
}
