using Application.Interfaces;
using Application.Services;
using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Reporting.NETCore;
using static Domain.DTO.IngresoProductosDTO;

namespace Comercial_Web.Pages.Productos.IngresoProductos
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        IProveedorService _proveedorService;
        IPorcentajeIvaService _porcentajeIvaService;
        IIngresoProductosService _ingresoProductosService;
        IParametroService _parametroService;
        IOrdenCompraService _ordenCompraService;
        ComercialDbContext _context;
        public List<ProovedoresSelectProductoDTO> Proveedores { get; set; } = new();
        [BindProperty] public int TipoBusqueda { get; set; }
        public IndexModel(IParametroService parametroService, IProveedorService proveedorService, IIngresoProductosService ingresoProductosService, 
                            IPorcentajeIvaService porcentajeIvaService, IOrdenCompraService ordenCompraService, ComercialDbContext context)
        {           
            _proveedorService = proveedorService;
            _porcentajeIvaService = porcentajeIvaService;
            _ingresoProductosService = ingresoProductosService;
            _parametroService = parametroService;
            _ordenCompraService = ordenCompraService;
            _context = context;
        }
     
        public List<IvaPorcentaje> IvaProcentajes { get; set; } = new();
        public async Task OnGet()
        {
            await CargarCombosAsync();
            await estadoInicial();
        }

        private async Task estadoInicial()
        {
            TipoBusqueda = await _parametroService.ObtenerIndiceBusquedaNotaPedidoAsync();
        }
        private async Task CargarCombosAsync()
        {
            Proveedores = (await _proveedorService.GetAllAsync())
                          .Select(v => new ProovedoresSelectProductoDTO
                          {
                              Id = v.Id,
                              NombreComercial = v.NombreComercial ?? "",
                              Ganancia = v.Ganancia ?? 0,
                              Descuento = v.Descuento ?? 0
                          }).ToList();

            IvaProcentajes = await _porcentajeIvaService.GetAllAsync();
                
        }
             
        public async Task<IActionResult> OnGetProductosPorProveedorAsync(int proveedorId)
        {
            var cantStock = await _parametroService.ObtenerCantidadDecimalesStockAsync();
            var cantDec = await _parametroService.ObtenerCantidadDecimalesProductosAsync();
            var productos = (await _ingresoProductosService.TraerProductosProveedorAsync(proveedorId, cantDec, cantStock));                

            return new  JsonResult(productos);
        }

        public async Task<IActionResult> OnGetOrdenCompraProveedorAsync(int proveedorId)
        {
            var cantDec = await _parametroService.ObtenerCantidadDecimalesProductosAsync();
            var pedidos = (await _ingresoProductosService.TraerOrdenesCompraSinProcesarPorProveedor(proveedorId, cantDec));

            return new JsonResult(pedidos);
        }

        public async Task<IActionResult> OnGetOrdenCompraProveedorFechaAsync(int proveedorId, DateTime desde, DateTime hasta)
        {
            var cantDec = await _parametroService.ObtenerCantidadDecimalesProductosAsync();
            var pedidos = (await _ingresoProductosService.TraerOrdenesCompraSinProcesarPorProveedorFecha(proveedorId, desde, hasta, cantDec));

            return new JsonResult(pedidos);
        }

        public async Task<IActionResult> OnGetOrdenCompraDetalleAsync(int ordenId)
        {
            var cantStock = await _parametroService.ObtenerCantidadDecimalesStockAsync();
            var cantDec = await _parametroService.ObtenerCantidadDecimalesProductosAsync();
            var detalle = (await _ingresoProductosService.TraerOrdenCompraDetalle(ordenId, cantDec, cantStock));

            return new JsonResult(detalle);
        }

        public async Task<IActionResult> OnGetImprimirOrdenCompraAsync(int id, string formato)
        {
            var datos = await _ordenCompraService.OrdenCompraImprimirAsync(id);
            var localReport = new LocalReport();
            localReport.ReportPath = Path.Combine(Directory.GetCurrentDirectory(), "Reports", "ReportOrdenCompra.rdlc");
            localReport.DataSources.Add(new ReportDataSource("DataSetOrdenCompra", datos));
            int cantDec = await _parametroService.ObtenerCantidadDecimalesProductosAsync();
            int cantStock = await _parametroService.ObtenerCantidadDecimalesStockAsync();

            var parameters = new[]
            {
                new ReportParameter("empresa", "Mi Empresa"),
                new ReportParameter("cantDec", cantDec.ToString()),
                new ReportParameter("cantStock", cantStock.ToString())
            };
            localReport.SetParameters(parameters);

            string deviceInfo = $@"<DeviceInfo>
                     <OutputFormat>{formato}</OutputFormat>
                     <PageWidth>21cm</PageWidth>
                     <PageHeight>29.7cm</PageHeight>
                     <MarginTop>0cm</MarginTop>
                     <MarginLeft>0cm</MarginLeft>
                     <MarginRight>0cm</MarginRight>
                     <MarginBottom>0cm</MarginBottom>
                   </DeviceInfo>";

            byte[] bytes = localReport.Render(formato, deviceInfo);

            string extension = formato.ToLower() switch
            {
                "excel" => "xlsx",
                "wordopenxml" => "docx",
                _ => "pdf"
            };

            string fileName = $"OrdenCompra_{id}_{DateTime.Now:ddMMyyyyHHmm}.{extension}";

            string contentType = formato.ToLower() switch
            {
                "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "wordopenxml" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/pdf"
            };

            return File(bytes, contentType, fileName);

        }

        public async Task<IActionResult> OnGetEliminarNPAsync(long notaId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await _ordenCompraService.EliminarOrdenAsync(notaId);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new JsonResult(new { success = true, id = notaId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest($"Error al Eliminar: {ex.Message}");
            }

        }
        
        public async Task<IActionResult> OnPostGuardarAsync([FromBody] List<IngresoProductoDetalleDto> detalle)
        {
            var progreso = new Progress<int>(p =>
            {
                HttpContext.Items["Progreso"] = p;
            });

            await _ingresoProductosService.ProcesarIngresoAsync(detalle, progreso);

            return new JsonResult(new { success = true });
        }
    }
}
