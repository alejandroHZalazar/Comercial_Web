using Application.Interfaces;
using Domain.Contracts;
using Domain.DTO;
using Domain.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Reporting.NETCore;


namespace Comercial_Web.Pages.Estadisticas.Ventas
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IVentasEstadisticasService _estadisticasService;
        private readonly IClienteService _clienteService;
        private readonly IUsuarioService _usuarioService;
        private readonly IProveedorService _proveedorService;

        public IndexModel(
            IVentasEstadisticasService estadisticasService,
            IClienteService clienteService,
            IUsuarioService usuarioService,
            IProveedorService proveedorService)
        {
            _estadisticasService = estadisticasService;
            _clienteService = clienteService;
            _usuarioService = usuarioService;
            _proveedorService = proveedorService;
        }

        [BindProperty] public DateTime Desde { get; set; } = DateTime.Today.AddDays(-30);
        [BindProperty] public DateTime Hasta { get; set; } = DateTime.Today;
        [BindProperty] public int? ClienteId { get; set; }
        [BindProperty] public int? VendedorId { get; set; }
        [BindProperty] public int? ProveedorId { get; set; }
        [BindProperty] public long? VentaSeleccionadaId { get; set; }
        public List<VentaDetalleDto> Detalle { get; set; } = new();



        public List<VentaResumenDto> Ventas { get; set; } = new();
        public List<SelectListItem> Clientes { get; set; } = new();
        public List<SelectListItem> Vendedores { get; set; } = new();
        public List<SelectListItem> Proveedores { get; set; } = new();

        // Totales (como en WinForms)
        public int Cantidad { get; set; }
        public decimal TotVentaSinIva { get; set; }
        public decimal TotCosto { get; set; }
        public decimal TotComision { get; set; }

        public async Task OnGetAsync() => await CargarCombosAsync();

        public async Task<IActionResult> OnPostBuscarAsync()
        {
            await CargarCombosAsync();

            Ventas = await _estadisticasService.GetResumenVentasAsync(Desde, Hasta, ClienteId, VendedorId, ProveedorId);

            // Procesar totales (igual que WinForms)
            Cantidad = Ventas.Count;
            TotVentaSinIva = Ventas.Sum(v => v.totalSIVA);
            TotCosto = Ventas.Sum(v => v.Costo);
            TotComision = Ventas.Sum(v => v.Comision);

            return Page();
        }

        private async Task CargarCombosAsync()
        {
            var clientes = await _clienteService.GetAllAsync();
            Clientes = clientes
                .OrderBy(c => c.NombreComercial)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.NombreComercial })
                .ToList();
            Clientes.Insert(0, new SelectListItem { Value = "", Text = "(Todos)" });

            var usuarios = await _usuarioService.GetAllAsync();
            Vendedores = usuarios
                .OrderBy(u => u.Nombre)
                .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Nombre ?? "" })
                .ToList();
            Vendedores.Insert(0, new SelectListItem { Value = "", Text = "(Todos)" });

            var proveedores = await _proveedorService.GetAllAsync();
            Proveedores = proveedores
                .OrderBy(p => p.NombreComercial)
                .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.NombreComercial ?? "" })
                .ToList();
            Proveedores.Insert(0, new SelectListItem { Value = "", Text = "(Todos)" });
        }

        public async Task<PartialViewResult> OnGetDetalleAsync(long id)
        {
            var detalle = await _estadisticasService.GetDetalleVentaAsync(id);
            var vm = new VentaDetalleViewModel { VentaId = id, Items = detalle };
            return Partial("_DetalleVenta", vm);


        }
        public async Task<IActionResult> OnGetImprimirAsync(string formato)
        {
            var ventas = await _estadisticasService.GetResumenVentasAsync(
                Desde, Hasta, ClienteId, VendedorId, ProveedorId);

            var localReport = new LocalReport();
            localReport.ReportPath = Path.Combine(Directory.GetCurrentDirectory(), "Reports", "ReportVentasEstadisticas.rdlc");

            localReport.DataSources.Add(new ReportDataSource("dsVentasEst", ventas));

            string subtitulo = $"Periodo desde {Desde:dd/MM/yyyy} al {Hasta:dd/MM/yyyy}";
            var parameters = new[] { new ReportParameter("subtitulo", subtitulo) };
            localReport.SetParameters(parameters);

            // Márgenes en 0 y vertical
            string deviceInfo = @"<DeviceInfo>
                                    <OutputFormat>PDF</OutputFormat>
                                    <PageWidth>21cm</PageWidth>
                                    <PageHeight>29.7cm</PageHeight>
                                    <MarginTop>0cm</MarginTop>
                                    <MarginLeft>0cm</MarginLeft>
                                    <MarginRight>0cm</MarginRight>
                                    <MarginBottom>0cm</MarginBottom>
                                </DeviceInfo>";

            // Render según formato
            byte[] bytes = localReport.Render(formato, deviceInfo);

            // Nombre dinámico
            string extension = formato.ToLower() switch
            {
                "excel" => "xlsx",
                "wordopenxml" => "docx",
                _ => "pdf"
            };

            string fileName = $"RankingVentas{DateTime.Now:ddMMyyyyHHmm}.{extension}";

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