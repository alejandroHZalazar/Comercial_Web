using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Domain.Contracts;
using Microsoft.AspNetCore.Authorization;
using Domain.DTO;
using Domain.Entities;
using System.Globalization;
using Application.Services;
using Microsoft.Reporting.NETCore;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Comercial_Web.Pages.Proveedores.OrdenesCompra
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly IProveedorService _proveedorService;
        private readonly IPorcentajeIvaService _ivaService;
        private readonly IParametroService _parametroService;
        private readonly IProductoService _productoService;
        private readonly IOrdenCompraService _ordenCompraService;
        private readonly ComercialDbContext _context;

        public IndexModel(
            IProveedorService proveedorService,
            IPorcentajeIvaService ivaService,
            IParametroService parametroService,
            IProductoService productoService,
            IOrdenCompraService ordenCompraService, 
            ComercialDbContext context)
        {
            _proveedorService = proveedorService;
            _ivaService = ivaService;
            _parametroService = parametroService;
            _productoService = productoService;
            _ordenCompraService = ordenCompraService;
            _context = context;
        }

        public SelectList Proveedores { get; set; } = null!;
        public List<ProductoDto> ProductosProveedor { get; set; } = new();
        public List<SelectListItem> Ivas { get; set; } = new();
        public bool Cargado { get; set; } = false;
        public decimal CantidadStep { get; set; } = 1;        

        [BindProperty] public int? ProveedorId { get; set; }
        [BindProperty] public int? IvaId { get; set; }
        [BindProperty] public int TipoBusqueda { get; set; }
        [BindProperty] public decimal Descuento { get; set; } = 0;
        [BindProperty] public decimal Recargo { get; set; } = 0;        
        [BindProperty] public int? ProductoSeleccionado { get; set; }
        [BindProperty] public List<OrdenCompraDetalle> Detalles { get; set; } = new();
        
        [BindProperty]  public decimal TotalGeneral {get; set;}

        [BindProperty] public decimal IvaValor { get; set; } = 0;
        [BindProperty] public bool ProveedorConfirmado { get; set; } = false;


        public async Task OnGetAsync()
        {
            var proveedores = await _proveedorService.TraerCabeceraAsync();
            var ivas = await _ivaService.GetAllAsync();
            TipoBusqueda = await _parametroService.ObtenerIndiceBusquedaNotaPedidoAsync();
            CantidadStep = await _parametroService.ObtenerCantidadDecimalesStockAsync();

            Proveedores = new SelectList(proveedores, "Id", "NombreComercial");

            Ivas = ivas.Select(i => new SelectListItem
            {
                Value = i.Valor.GetValueOrDefault()
                   .ToString(CultureInfo.InvariantCulture),

                Text = i.Valor.GetValueOrDefault()
                  .ToString("0.###", CultureInfo.CurrentCulture)
            }).ToList();



            if (IvaId.HasValue)
            {
                var iva = await _ivaService.GetByIdAsync(IvaId.Value);
                IvaValor = iva?.Valor ?? 0m;
            }


            Cargado = true;
        }

        public async Task<IActionResult> OnGetProductosAsync(int proveedorId)
        {
            var productos = await _productoService.TraerProductosProveedorAsync(proveedorId);

            // Guardar en la propiedad si querés usarla en otros handlers
            ProductosProveedor = productos;

            // Extraer solo las descripciones
            var descripciones = productos
                .Where(p => !string.IsNullOrWhiteSpace(p.Descripcion))
                .Select(p => p.Descripcion)
                .Distinct()
                .ToList();

            return new JsonResult(descripciones);

        }

        public async Task<IActionResult> OnGetBuscarProductoAsync(string filtro, int tipoBusqueda, int proveedorId)
        {
            ProductoLineaOCDto? producto = null;

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                if (tipoBusqueda == 1)
                    producto = await _ordenCompraService.BuscarPorCodProveedorOCAsync(filtro, proveedorId);
                else if (tipoBusqueda == 2)
                    producto = await _ordenCompraService.BuscarPorCodBarrasOCAsync(filtro, proveedorId);
                else
                    producto = await _ordenCompraService.TraerPorIdOCAsync(int.Parse(filtro));
            }

            if (producto != null)
            {
                return new JsonResult(new
                {
                    encontrado = true,
                    id = producto.Id,
                    codProveedor = producto.CodProveedor,
                    codBarras = producto.CodBarras,
                    descripcion = producto.Descripcion,
                    cantidad = producto.Cantidad,
                    cantidadMinima = producto.CantidadMinima,
                    precio = producto.PrecioProveedor
                });
            }

            else
            {
                return new JsonResult(new { encontrado = false, mensaje = $"No existe el producto: {filtro}" });
            }
        }

        public async Task<IActionResult> OnGetBuscarPorDescripcionAsync(string descripcion, int proveedorId)
        {
            if (string.IsNullOrWhiteSpace(descripcion))
                return new JsonResult(new { encontrado = false, mensaje = "Debe ingresar una descripción válida." });

            var producto = await _ordenCompraService.BuscarPorDescripcionExactaAsync(descripcion, proveedorId);

            if (producto != null)
            {
                return new JsonResult(new
                {
                    encontrado = true,
                    id = producto.Id,
                    codProveedor = producto.CodProveedor,
                    codBarras = producto.CodBarras,
                    descripcion = producto.Descripcion,
                    cantidad = producto.Cantidad,
                    cantidadMinima = producto.CantidadMinima,
                    precio = producto.PrecioProveedor
                });
            }
            else
            {
                return new JsonResult(new { encontrado = false, mensaje = $"No existe el producto con descripción: {descripcion}" });
            }
        }
        
        public async Task<IActionResult> OnGetPrepararProductoAsync(string descripcion, int proveedorId)
        {
            var producto = await _ordenCompraService.BuscarPorDescripcionExactaAsync(descripcion, proveedorId);
            if (producto != null)
            {
                return Partial("_ProductoSeleccionado", producto);
            }
            return Content("<div class='alert alert-danger'>No existe el producto</div>");
        }

        public async Task<IActionResult> OnGetRecalcularTotalesAsync(int ivaId)
        {
            CalcularTotales(IvaValor);
            return Partial("_Totales", TotalGeneral);

        }

        public async Task<JsonResult> OnGetCantMinimaAsync(int proveedorId)
        {
            var productos = await _ordenCompraService.TraerCantMinPorProveedorAsync(proveedorId);
            return new JsonResult(productos);
        }

        public void ProcesoTotales()
        {

            CalcularTotales(IvaValor);
        }

        private void CalcularTotales(decimal ivaValor)
        {
            decimal total = 0;

            foreach (var fila in Detalles)
            {
                var costoOrig = fila.PrecioProveedor ?? 0;

                decimal precioSinIva = costoOrig;
                if (Descuento > 0)
                {
                    precioSinIva = Math.Round(((100 - Descuento) * costoOrig / 100), 2);
                    Recargo = 0;
                }
                else if (Recargo > 0)
                {
                    precioSinIva = Math.Round(((100 + Recargo) * costoOrig / 100), 2);
                    Descuento = 0;
                }

                var precioConIva = Math.Round(precioSinIva * (1 + (ivaValor / 100)), 2);

                fila.Subtotal = Math.Round(precioConIva * (fila.Cantidad ?? 0), 2);

                total += fila.Subtotal ?? 0;
            }

            TotalGeneral = Math.Round(total, 2);
        }

        public async Task<JsonResult> OnGetProductosAPedirAsync(int proveedorId, DateTime desde, DateTime hasta)
        {
            var productos = await _ordenCompraService.TraerListaProdAPedirAsync(proveedorId, desde, hasta);
            return new JsonResult(productos);
        }

        public async Task<IActionResult> OnGetImprimirStockProductosAsync(string formato, DateTime desde, DateTime hasta, int proveedorId)
        {
            var productos = await _ordenCompraService.TraerListaProdAPedirImprimirAsync(proveedorId, desde, hasta);

            var localReport = new LocalReport();
            localReport.ReportPath = Path.Combine(Directory.GetCurrentDirectory(), "Reports", "ReportProductoPedir.rdlc");

            localReport.DataSources.Add(new ReportDataSource("dsProductosPedir", productos));

            string subtitulo = $"MOVIMIENTOS DE PRODUCTOS DESDE {desde:dd/MM/yyyy} HASTA {hasta:dd/MM/yyyy}";
            int cantDec = await _parametroService.ObtenerCantidadDecimalesProductosAsync();
            int cantStock = await _parametroService.ObtenerCantidadDecimalesStockAsync();

            var parameters = new[]
            {
                new ReportParameter("subtitulo", subtitulo),
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

            string fileName = $"ProductosAPedir_{DateTime.Now:ddMMyyyyHHmm}.{extension}";

            string contentType = formato.ToLower() switch
            {
                "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "wordopenxml" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/pdf"
            };

            return File(bytes, contentType, fileName);
        }

        
        public async Task<IActionResult> OnPostGrabarAsync()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

             var request = JsonSerializer.Deserialize<OrdenCompraDTO>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (request.Detalles == null || !request.Detalles.Any(d => d.Cantidad > 0))
                return BadRequest("Debe ingresar al menos un pedido válido.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var cabecera = new OrdenCompra
                {
                    Fecha = DateTime.Now,
                    FkProveedor = request.ProveedorId,
                    Total = request.Total,
                    Procesado = false,
                    Iva = request.Iva,
                    Recargo = request.Recargo,
                    Descuento = request.Descuento
                };

                _context.OrdenCompras.Add(cabecera);
                await _context.SaveChangesAsync();

                foreach (var d in request.Detalles.Where(x => x.Cantidad > 0))
                {
                    d.FkOrdenCompra = cabecera.Id;
                    d.Procesado = false;
                    _context.OrdenCompraDetalles.Add(d);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new JsonResult(new { success = true, id = cabecera.Id });
                
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest($"Error al grabar: {ex.Message}");
            }
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

    }

}