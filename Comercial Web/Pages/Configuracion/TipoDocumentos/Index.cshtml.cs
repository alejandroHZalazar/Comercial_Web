using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Comercial_Web.Pages.Configuracion.TipoDocumentos
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IDocumentoTipoService _service;
        public IndexModel(IDocumentoTipoService service) => _service = service;

        // ====== Grilla ======
        public List<DocumentoTipo> Items { get; private set; } = new();

        // ====== Modal ======
        [BindProperty] public int? Id { get; set; }
        [BindProperty] public string Abreviatura { get; set; } = string.Empty;
        [BindProperty] public string? Descripcion { get; set; }
        public bool MostrarModal { get; set; }

        // =====================

        public async Task OnGetAsync()
        {
            await CargarGrillaAsync();
        }

        public async Task<IActionResult> OnPostNuevoAsync()
        {
            MostrarModal = true;
            Id = null;
            Abreviatura = string.Empty;
            Descripcion = string.Empty;

            await CargarGrillaAsync();
            return Page();
        }

        public async Task<IActionResult> OnGetEditarAsync(int id)
        {
            await CargarGrillaAsync();

            var item = await _service.GetByIdAsync(id);
            if (item is null) return NotFound();

            Id = item.Id;
            Abreviatura = item.Abreviatura;
            Descripcion = item.Descripcion;

            MostrarModal = true;
            return Page();
        }

        public async Task<IActionResult> OnPostAgregarAsync()
        {
            await _service.CreateAsync(Abreviatura, Descripcion);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditarAsync()
        {
            if (!Id.HasValue) return BadRequest();
            await _service.UpdateAsync(Id.Value, Abreviatura, Descripcion);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEliminarAsync(int id)
        {
            await _service.DeleteAsync(id);
            return RedirectToPage();
        }

        private async Task CargarGrillaAsync()
        {
            Items = await _service.GetAllAsync();
        }
    }
}
