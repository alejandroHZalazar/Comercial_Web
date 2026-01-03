using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Comercial_Web.Pages.Configuracion.Rubros
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IRubroService _service;
        public IndexModel(IRubroService service) => _service = service;

        public List<Rubro> Items { get; private set; } = new();

        [BindProperty] public int? Id { get; set; }
        [BindProperty] public string Descripcion { get; set; } = string.Empty;

       public bool MostrarFormulario { get; set; }

        public async Task OnGetAsync()
        {
            Items = await _service.GetAllAsync();
        }

        public async Task<IActionResult> OnGetEditarAsync(int id)
        {
            Items = await _service.GetAllAsync();

            var rubro = await _service.GetByIdAsync(id);
            if (rubro is null) return NotFound();

            Id = rubro.Id;
            Descripcion = rubro.Descripcion;
            MostrarFormulario = true; 

            return Page();
        }

        public async Task<IActionResult> OnPostNuevoAsync()
        {            
            MostrarFormulario = true;
            Items = await _service.GetAllAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostGuardarNuevoAsync()
        {
            if (!FormularioValido())
            {
                Items = await _service.GetAllAsync();
                MostrarFormulario = true; 
                return Page();
            }

            await _service.CreateAsync(Descripcion);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostGuardarEdicionAsync()
        {
            if (!Id.HasValue || !FormularioValido())
            {
                Items = await _service.GetAllAsync();
                MostrarFormulario = true;
                return Page();
            }

            await _service.UpdateAsync(Id.Value, Descripcion);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEliminarAsync(int id)
        {
            await _service.DeleteAsync(id);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCancelarAsync()
        {           
            return RedirectToPage();
        }

        private bool FormularioValido()
        {
            ModelState.Clear();

            if (string.IsNullOrWhiteSpace(Descripcion))
            {
                ModelState.AddModelError(nameof(Descripcion), "Debe indicar una descripción");
                return false;
            }

            return true;
        }
    }
}