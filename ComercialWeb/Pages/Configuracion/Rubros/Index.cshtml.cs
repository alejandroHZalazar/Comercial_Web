using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Comercial_Web.Pages.Configuracion.Rubros
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IRubroService _service;
        public IndexModel(IRubroService service) => _service = service;

        public List<RubroNodoDto> Items { get; private set; } = new();
        public List<SelectListItem> Padres { get; private set; } = new();

        [BindProperty] public int? Id { get; set; }
        [BindProperty] public string Descripcion { get; set; } = string.Empty;
        [BindProperty] public int? FkRubroPadre { get; set; }

        [TempData] public string? Error { get; set; }

        public bool MostrarFormulario { get; set; }

        public async Task OnGetAsync()
        {
            await CargarAsync();
        }

        public async Task<IActionResult> OnGetEditarAsync(int id)
        {
            var rubro = await _service.GetByIdAsync(id);
            if (rubro is null) return NotFound();

            Id = rubro.Id;
            Descripcion = rubro.Descripcion ?? string.Empty;
            FkRubroPadre = rubro.FkRubroPadre;
            MostrarFormulario = true;

            await CargarAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostNuevoAsync()
        {
            MostrarFormulario = true;
            await CargarAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostGuardarNuevoAsync()
        {
            if (!FormularioValido())
                return await VolverAlFormularioAsync();

            try
            {
                await _service.CreateAsync(Descripcion, PadreNormalizado());
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                ModelState.AddModelError(nameof(Descripcion), ex.Message);
                return await VolverAlFormularioAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostGuardarEdicionAsync()
        {
            if (!Id.HasValue || !FormularioValido())
                return await VolverAlFormularioAsync();

            try
            {
                await _service.UpdateAsync(Id.Value, Descripcion, PadreNormalizado());
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                ModelState.AddModelError(nameof(Descripcion), ex.Message);
                return await VolverAlFormularioAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEliminarAsync(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
            }
            catch (InvalidOperationException ex)
            {
                Error = ex.Message;
            }
            return RedirectToPage();
        }

        public IActionResult OnPostCancelar()
        {
            return RedirectToPage();
        }

        private int? PadreNormalizado() => FkRubroPadre is > 0 ? FkRubroPadre : null;

        private async Task<IActionResult> VolverAlFormularioAsync()
        {
            MostrarFormulario = true;
            await CargarAsync();
            return Page();
        }

        // Grilla + opciones del combo "Rubro padre". Al editar se excluyen el propio rubro y sus
        // subrubros (el SP también lo valida, esto solo evita ofrecer opciones inválidas).
        private async Task CargarAsync()
        {
            Items = await _service.GetAllConRutaAsync();

            var excluidos = new HashSet<int>();
            if (Id.HasValue)
            {
                excluidos.Add(Id.Value);
                var agregado = true;
                while (agregado)
                {
                    agregado = false;
                    foreach (var n in Items)
                        if (n.FkRubroPadre.HasValue && excluidos.Contains(n.FkRubroPadre.Value) && excluidos.Add(n.Id))
                            agregado = true;
                }
            }

            Padres = new List<SelectListItem> { new("(Rubro principal)", "") };
            Padres.AddRange(Items
                .Where(n => !excluidos.Contains(n.Id))
                .Select(n => new SelectListItem(n.Ruta, n.Id.ToString())));
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
