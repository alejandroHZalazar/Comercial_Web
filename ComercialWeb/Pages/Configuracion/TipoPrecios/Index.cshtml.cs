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
        private readonly IParametroService _parametroService;

        public IndexModel(ITipoPrecioService service, IParametroService parametroService)
        {
            _service = service;
            _parametroService = parametroService;
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

        // ====== Cotización Dólar ======
        public bool DolarizaProductos { get; set; }
        public decimal CotizacionDolar { get; set; }
        [BindProperty] public string? NuevaCotizacion { get; set; }

        // =====================

        public async Task OnGetAsync()
        {
            await CargarGrillaAsync();
            await CargarComboAsync();
            await CargarParametrosDolarAsync();
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
            await CargarParametrosDolarAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostNuevoAsync()
        {

            MostrarFormulario = true;
            await CargarGrillaAsync();
            await CargarComboAsync();
            await CargarParametrosDolarAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostGuardarNuevoAsync()
        {
            if (!FormularioValido())
            {
                await CargarGrillaAsync();
                await CargarComboAsync();
                await CargarParametrosDolarAsync();
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
                await CargarParametrosDolarAsync();
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

        public async Task<IActionResult> OnPostActualizarCotizacionAsync()
        {
            // Normalizar: reemplazar coma por punto para parsear siempre con InvariantCulture
            var valorNormalizado = (NuevaCotizacion ?? "0").Replace(',', '.');
            if (decimal.TryParse(valorNormalizado, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var cotizacion))
            {
                await _parametroService.ActualizarValorAsync("productos", "cotizacionDolar",
                    cotizacion.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
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

        private async Task CargarParametrosDolarAsync()
        {
            var dolariza = await _parametroService.ObtenerValorAsync("productos", "dolarizaProductos");
            DolarizaProductos = int.TryParse(dolariza, out var val) && val != 0;

            var cotizacion = await _parametroService.ObtenerValorAsync("productos", "cotizacionDolar");
            CotizacionDolar = decimal.TryParse(cotizacion, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var cot) ? cot : 0;
        }

        private bool FormularioValido()
        {
            if (string.IsNullOrWhiteSpace(Descripcion))
            {
                ModelState.AddModelError(nameof(Descripcion),
                    "Debe indicar una descripci�n");
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