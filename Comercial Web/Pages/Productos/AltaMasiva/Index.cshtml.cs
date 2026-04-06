using Application.Interfaces;
using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;

namespace Comercial_Web.Pages.Productos.AltaMasiva
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly IAltaMasivaProductosService _altaMasivaService;
        private readonly IRubroService               _rubroService;
        private readonly IProveedorService           _proveedorService;

        public List<SelectListItem> Rubros      { get; private set; } = new();
        public List<SelectListItem> Proveedores { get; private set; } = new();

        public IndexModel(
            IAltaMasivaProductosService altaMasivaService,
            IRubroService rubroService,
            IProveedorService proveedorService)
        {
            _altaMasivaService = altaMasivaService;
            _rubroService      = rubroService;
            _proveedorService  = proveedorService;
        }

        public async Task OnGetAsync()
        {
            Rubros = (await _rubroService.GetAllAsync())
                .Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Descripcion })
                .ToList();

            Proveedores = (await _proveedorService.GetAllAsync())
                .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.NombreComercial ?? "" })
                .ToList();
        }

        // POST: procesa una fila individual (llamado N veces desde JS)
        public async Task<JsonResult> OnPostProcesarFilaAsync([FromBody] AltaMasivaFilaDto fila)
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            if (fila is null)
                return new JsonResult(new AltaMasivaResultDto { Success = false, Mensaje = "Datos inválidos." }, options);

            // Validación: duplicado
            bool existe = await _altaMasivaService.ExisteProductoAsync(fila.CodProveedor, fila.FkProveedor);
            if (existe)
            {
                return new JsonResult(new AltaMasivaResultDto
                {
                    Success = false,
                    Mensaje = $"DUPLICADO — Ya existe un producto con Cód. Proveedor «{fila.CodProveedor}» para ese proveedor. No fue cargado."
                }, options);
            }

            var resultado = await _altaMasivaService.CrearProductoAsync(fila);
            return new JsonResult(resultado, options);
        }
    }
}
