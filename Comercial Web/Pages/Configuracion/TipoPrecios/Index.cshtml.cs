using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Comercial_Web.Pages.Configuracion.TipoPrecios
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ITipoPrecioService _service;

        public IndexModel(ITipoPrecioService service)
        {
            _service = service;
        }

        // ====== Grilla ======
        public List<TipoPrecioGridItem> Items { get; private set; } = new();

        // ====== Form ======
        [BindProperty] public int? Id { get; set; }
        [BindProperty] public string Descripcion { get; set; } = string.Empty;
        [BindProperty] public int? FkTipoValor { get; set; }
        [BindProperty] public decimal? Valor { get; set; }

        public List<SelectListItem> TiposValor { get; private set; } = new();

        
        public bool MostrarFormulario { get; set; }

        // =====================

        public async Task OnGetAsync()
        {
            await CargarGrillaAsync();
            await CargarComboAsync();
        }

        public async Task<IActionResult> OnGetEditarAsync(int id)
        {
            await CargarGrillaAsync();
            await CargarComboAsync();

            var entity = await _service.GetByIdAsync(id);
            if (entity is null)
                return NotFound();

            Id = entity.Id;
            Descripcion = entity.Descripcion ?? string.Empty;
            FkTipoValor = entity.FkTipoValor;
            Valor = entity.Valor;

            MostrarFormulario = true; 

            return Page();
        }

        public async Task<IActionResult> OnPostNuevoAsync()
        {
           
            MostrarFormulario = true;
            await CargarGrillaAsync();
            await CargarComboAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostGuardarNuevoAsync()
        {
            if (!FormularioValido())
            {
                await CargarGrillaAsync();
                await CargarComboAsync();
                MostrarFormulario = true; 
                return Page();
            }

            await _service.CreateAsync(
                Descripcion.Trim(),
                FkTipoValor!.Value,
                Valor!.Value
            );

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostGuardarEdicionAsync()
        {
            if (!Id.HasValue || !FormularioValido())
            {
                await CargarGrillaAsync();
                await CargarComboAsync();
                MostrarFormulario = true; 
                return Page();
            }

            await _service.UpdateAsync(
                Id.Value,
                Descripcion.Trim(),
                FkTipoValor!.Value,
                Valor!.Value
            );

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

        // ====== Helpers ======

        private async Task CargarGrillaAsync()
        {
            var tiposPrecio = await _service.GetAllAsync();
            var tiposValor = await _service.GetTiposValorAsync();

            Items = tiposPrecio.Select(tp => new TipoPrecioGridItem
            {
                Id = tp.Id,
                Descripcion = tp.Descripcion ?? string.Empty,
                Valor = tp.Valor,
                TipoValorDescripcion = tiposValor
                    .FirstOrDefault(tv => tv.Id == tp.FkTipoValor)?.Descripcion ?? ""
            }).ToList();
        }

        private async Task CargarComboAsync()
        {
            var tiposValor = await _service.GetTiposValorAsync();

            TiposValor = tiposValor
                .Select(tv => new SelectListItem
                {
                    Value = tv.Id.ToString(),
                    Text = tv.Descripcion
                })
                .ToList();
        }

        private bool FormularioValido()
        {
            if (string.IsNullOrWhiteSpace(Descripcion))
            {
                ModelState.AddModelError(nameof(Descripcion),
                    "Debe indicar una descripción");
                return false;
            }

            if (!FkTipoValor.HasValue)
            {
                ModelState.AddModelError(nameof(FkTipoValor),
                    "Debe seleccionar un tipo de valor");
                return false;
            }

            return true;
        }
    }

    // ===== DTO interno para la grilla =====
    public class TipoPrecioGridItem
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string TipoValorDescripcion { get; set; } = string.Empty;
        public decimal? Valor { get; set; }
    }
}