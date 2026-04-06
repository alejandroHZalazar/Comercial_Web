using Application.Interfaces;
using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Comercial_Web.Pages.Estadistica.DashboardVentas;

[Authorize]
[ValidateAntiForgeryToken]
public class IndexModel : PageModel
{
    private readonly IDashboardVentasService    _ventasService;
    private readonly IDashboardProveedorService _proveedorService;
    private readonly IDashboardClienteService   _clienteService;
    private readonly IProveedorService          _proveedores;
    private readonly IClienteService            _clientes;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public IndexModel(
        IDashboardVentasService    ventasService,
        IDashboardProveedorService proveedorService,
        IDashboardClienteService   clienteService,
        IProveedorService          proveedores,
        IClienteService            clientes)
    {
        _ventasService    = ventasService;
        _proveedorService = proveedorService;
        _clienteService   = clienteService;
        _proveedores      = proveedores;
        _clientes         = clientes;
    }

    public void OnGet() { }

    // ====================================================================
    //  HANDLERS DASHBOARD VENTAS
    // ====================================================================

    public async Task<JsonResult> OnPostIndicadoresAsync([FromBody] FiltroRequest request)
    {
        try
        {
            var data = await _ventasService.GetIndicadoresAsync(request.Desde, request.Hasta);
            return new JsonResult(data, JsonOpts);
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = ex.Message }, JsonOpts) { StatusCode = 500 };
        }
    }

    public async Task<JsonResult> OnPostVentasAsync([FromBody] FiltroRequest request)
    {
        try
        {
            var data = await _ventasService.GetVentasAsync(request.Desde, request.Hasta);
            return new JsonResult(data, JsonOpts);
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = ex.Message }, JsonOpts) { StatusCode = 500 };
        }
    }

    // ====================================================================
    //  HANDLERS MODAL PROVEEDORES
    // ====================================================================

    // ====================================================================
    //  HANDLERS MODAL CLIENTES
    // ====================================================================

    /// <summary>Listado de clientes activos para el dropdown del modal.</summary>
    public async Task<JsonResult> OnGetClientesAsync()
    {
        var lista = (await _clientes.GetAllAsync())
            .Where(c => c.Baja != true)
            .OrderBy(c => c.NombreComercial)
            .Select(c => new { id = c.Id, nombre = c.NombreComercial ?? "?" })
            .ToList();

        return new JsonResult(lista, JsonOpts);
    }

    public async Task<JsonResult> OnPostClienteMetricasAsync([FromBody] ClienteFiltroRequest request)
    {
        try
        {
            var data = await _clienteService.GetMetricasAsync(
                request.Desde, request.Hasta, request.ClienteIds);
            return new JsonResult(data, JsonOpts);
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = ex.Message }, JsonOpts) { StatusCode = 500 };
        }
    }

    public async Task<JsonResult> OnPostClienteGraficosAsync([FromBody] ClienteFiltroRequest request)
    {
        try
        {
            var data = await _clienteService.GetGraficosAsync(
                request.Desde, request.Hasta, request.ClienteIds);
            return new JsonResult(data, JsonOpts);
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = ex.Message }, JsonOpts) { StatusCode = 500 };
        }
    }

    // ====================================================================
    //  HANDLERS MODAL PROVEEDORES
    // ====================================================================

    /// <summary>Listado de proveedores activos para el dropdown del modal.</summary>
    public async Task<JsonResult> OnGetProveedoresAsync()
    {
        var lista = (await _proveedores.GetAllAsync())
            .Where(p => p.Baja != true)
            .OrderBy(p => p.NombreComercial)
            .Select(p => new { id = p.Id, nombre = p.NombreComercial ?? "?" })
            .ToList();

        return new JsonResult(lista, JsonOpts);
    }

    public async Task<JsonResult> OnPostProveedorMetricasAsync([FromBody] ProveedorFiltroRequest request)
    {
        try
        {
            var data = await _proveedorService.GetMetricasAsync(
                request.Desde, request.Hasta, request.ProveedorIds);
            return new JsonResult(data, JsonOpts);
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = ex.Message }, JsonOpts) { StatusCode = 500 };
        }
    }

    public async Task<JsonResult> OnPostProveedorGraficosAsync([FromBody] ProveedorFiltroRequest request)
    {
        try
        {
            var data = await _proveedorService.GetGraficosAsync(
                request.Desde, request.Hasta, request.ProveedorIds);
            return new JsonResult(data, JsonOpts);
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = ex.Message }, JsonOpts) { StatusCode = 500 };
        }
    }

    // ====================================================================
    //  REQUEST MODELS
    // ====================================================================

    public class FiltroRequest
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
    }

    public class ProveedorFiltroRequest
    {
        public DateTime   Desde        { get; set; }
        public DateTime   Hasta        { get; set; }
        public List<int>? ProveedorIds { get; set; }
    }

    public class ClienteFiltroRequest
    {
        public DateTime   Desde      { get; set; }
        public DateTime   Hasta      { get; set; }
        public List<int>? ClienteIds { get; set; }
    }
}
