using Application.Interfaces;
using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Comercial_Web.Pages.Ventas.ReportePedidos;

[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly IPedidoService   _pedidoService;
    private readonly IUsuarioService  _usuarioService;
    private readonly IClienteService  _clienteService;

    public IndexModel(
        IPedidoService  pedidoService,
        IUsuarioService usuarioService,
        IClienteService clienteService)
    {
        _pedidoService  = pedidoService;
        _usuarioService = usuarioService;
        _clienteService = clienteService;
    }

    public List<SelectListItem> ListaVendedores { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var vendedores = await _usuarioService.GetAllAsync();
        ListaVendedores = vendedores
            .Where(u => u.Baja != true)
            .OrderBy(u => u.Nombre)
            .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Nombre ?? "" })
            .ToList();
    }

    public async Task<List<SelectListItem>> GetClientesListAsync()
    {
        var todos = await _clienteService.GetAllAsync();
        return todos
            .Where(c => c.Baja != true)
            .OrderBy(c => c.NombreComercial)
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.NombreComercial ?? "" })
            .ToList();
    }

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

    public async Task<IActionResult> OnGetPedidoAsync(int id)
    {
        var cab     = await _pedidoService.GetCabeceraAsync(id);
        if (cab == null) return new JsonResult(new { ok = false });
        var detalle = await _pedidoService.GetDetalleAsync(id);
        return new JsonResult(new { ok = true, cab, detalle });
    }

    public async Task<IActionResult> OnPostMarcarImpresoAsync([FromBody] MarcarImpresoDto dto)
    {
        if (dto == null || dto.PedidoId <= 0)
            return new JsonResult(new { ok = false, msg = "ID inválido." });
        try
        {
            await _pedidoService.MarcarImpresoAsync(dto.PedidoId);
            return new JsonResult(new { ok = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { ok = false, msg = ex.Message });
        }
    }
}

public class MarcarImpresoDto
{
    public int PedidoId { get; set; }
}
