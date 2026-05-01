using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Comercial_Web.Pages.Proveedores.ABM
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IProveedorService _proveedorService;

        public IndexModel(IProveedorService proveedorService)
        {
            _proveedorService = proveedorService;
        }

        public List<Proveedore> Proveedores { get; set; } = new();
        public Proveedore? ProveedorSeleccionado { get; set; }

        [BindProperty] public Proveedore ProveedorForm { get; set; } = new();

        public async Task OnGetAsync()
        {
            Proveedores = await _proveedorService.GetAllAsync();
        }

        public async Task<IActionResult> OnGetDetalleAsync(int id)
        {
            var proveedor = await _proveedorService.GetByIdAsync(id);
            if (proveedor == null)
                return Content("<p class='text-danger'>Proveedor no encontrado.</p>");

            return Partial("_DetalleProveedor", proveedor);
        }


        public async Task<IActionResult> OnGetFormModalAsync(int? id)
        {
            if (id.HasValue)
                ProveedorForm = await _proveedorService.GetByIdAsync(id.Value) ?? new Proveedore();
            else
                ProveedorForm = new Proveedore();

            return Partial("_FormProveedor", ProveedorForm);
        }

        public async Task<IActionResult> OnPostGuardarAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            if (ProveedorForm.Id == 0)
            {
                await _proveedorService.CreateAsync(
                    ProveedorForm.NombreComercial,
                    ProveedorForm.Cuil,
                    ProveedorForm.Direccion,
                    ProveedorForm.Email,
                    ProveedorForm.Telefono,
                    ProveedorForm.Celular,
                    ProveedorForm.Ganancia ?? 0m,
                    ProveedorForm.Descuento ?? 0m
                );
            }
            else
            {
                await _proveedorService.UpdateAsync(
                    ProveedorForm.Id,
                    ProveedorForm.NombreComercial,
                    ProveedorForm.Cuil,
                    ProveedorForm.Direccion,
                    ProveedorForm.Email,
                    ProveedorForm.Telefono,
                    ProveedorForm.Celular,
                    ProveedorForm.Ganancia ?? 0m,
                    ProveedorForm.Descuento ?? 0m
                );
            }

            return RedirectToPage();
        }


        public async Task<IActionResult> OnPostEliminarAsync(int id)
        {
            await _proveedorService.DeleteAsync(id);
            return RedirectToPage();
        }
    }
}