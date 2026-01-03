using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Comercial_Web.Pages.Configuracion.CondicionIva
{
    [Authorize] // exige login
    public class IndexModel : PageModel
    {
        private readonly ICondicionIvaService _service;
        public IndexModel(ICondicionIvaService service) => _service = service;

        public List<Domain.Entities.CondicionIva> Items { get; private set; } = new();

        [BindProperty] public int? Id { get; set; }
        [BindProperty] public string Descripcion { get; set; } = string.Empty;
        [BindProperty] public string Abrev { get; set; } = string.Empty;
        [BindProperty] public string Letra { get; set; } = string.Empty;
        [BindProperty] public bool MostrarFormulario { get; set; } = false;

        public async Task OnGetAsync()
        {
            Items = await _service.GetAllAsync();
        }

        public async Task<IActionResult> OnPostNuevoAsync()
        {
            // Mostrar formulario vacío para agregar
            MostrarFormulario = true;

            // Limpiar campos
            Id = null;
            Descripcion = string.Empty;
            Abrev = string.Empty;
            Letra = string.Empty;

            await CargarGrillaAsync();
            return Page();

        }

        public async Task<IActionResult> OnPostAgregarAsync()
        {
            await _service.CreateAsync(Descripcion, Abrev, Letra);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditarAsync()
        {
            if (!Id.HasValue) return BadRequest();
            await _service.UpdateAsync(Id.Value, Descripcion, Abrev, Letra);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEliminarAsync(int id)
        {
            await _service.DeleteAsync(id);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetEditarAsync(int id)
        {
            Items = await _service.GetAllAsync();

            var item = await _service.GetByIdAsync(id);
            if (item is null) return NotFound();

            // Cargar valores en el formulario
            Id = item.Id;
            Descripcion = item.Descripcion;
            Abrev = item.Abrev;
            Letra = item.Letra;

            // Activar formulario en modo edición
            MostrarFormulario = true;

            return Page();

        }

        private async Task CargarGrillaAsync()
        {
            Items = await _service.GetAllAsync();
        }

    }
}