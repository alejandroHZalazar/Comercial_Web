using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Comercial_Web.Pages.Configuracion.ConceptosCaja
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IConceptoCajaService _service;
        private readonly IMedioPagoService _medioPagoService;

        public IndexModel(IConceptoCajaService service, IMedioPagoService medioPagoService)
        {
            _service = service;
            _medioPagoService = medioPagoService;
        }

        // ====== Grilla ======
        public List<ConceptoCaja> Items { get; private set; } = new();

        // ====== Form (modal) ======
        [BindProperty] public int? Id { get; set; }
        [BindProperty] public string Nombre { get; set; } = string.Empty;
        [BindProperty] public string TipoMovimiento { get; set; } = "I";
        [BindProperty] public bool AfectaEfectivo { get; set; }
        [BindProperty] public bool AsociadaMedioPago { get; set; }
        [BindProperty] public int? FkMedioPago { get; set; }
        [BindProperty] public string? Operacion { get; set; }

        public bool MostrarModal { get; set; }
        public List<SelectListItem> MediosPago { get; private set; } = new();

        public static List<SelectListItem> TiposOperacion => new()
        {
            new SelectListItem("Ventas", "Ventas"),
            new SelectListItem("Cobros", "Cobros"),
            new SelectListItem("Pagos", "Pagos")
        };

        // =====================

        public async Task OnGetAsync()
        {
            await CargarGrillaAsync();
        }

        public async Task<IActionResult> OnGetEditarAsync(int id)
        {
            await CargarGrillaAsync();
            await CargarMediosPagoAsync();

            var item = await _service.GetByIdAsync(id);
            if (item is null) return NotFound();

            Id = item.Id;
            Nombre = item.Nombre;
            TipoMovimiento = item.TipoMovimiento;
            AfectaEfectivo = item.AfectaEfectivo;
            FkMedioPago = item.FkMedioPago;
            Operacion = item.Operacion;
            AsociadaMedioPago = item.FkMedioPago.HasValue;

            MostrarModal = true;
            return Page();
        }

        public async Task<IActionResult> OnPostNuevoAsync()
        {
            MostrarModal = true;
            Id = null;
            Nombre = string.Empty;
            TipoMovimiento = "I";
            AfectaEfectivo = false;
            AsociadaMedioPago = false;
            FkMedioPago = null;
            Operacion = null;

            await CargarGrillaAsync();
            await CargarMediosPagoAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAgregarAsync()
        {
            var medioPago = AsociadaMedioPago ? FkMedioPago : null;
            var operacion = AsociadaMedioPago ? Operacion : null;

            await _service.CreateAsync(Nombre, TipoMovimiento, AfectaEfectivo, medioPago, operacion);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditarAsync()
        {
            if (!Id.HasValue) return BadRequest();

            var medioPago = AsociadaMedioPago ? FkMedioPago : null;
            var operacion = AsociadaMedioPago ? Operacion : null;

            await _service.UpdateAsync(Id.Value, Nombre, TipoMovimiento, AfectaEfectivo, medioPago, operacion);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEliminarAsync(int id)
        {
            await _service.DeleteAsync(id);
            return RedirectToPage();
        }

        // ====== Helpers ======

        private async Task CargarGrillaAsync()
        {
            Items = await _service.GetAllAsync();
        }

        private async Task CargarMediosPagoAsync()
        {
            var medios = await _medioPagoService.GetAllAsync();
            MediosPago = medios.Select(m => new SelectListItem(m.Nombre, m.Id.ToString())).ToList();
        }
    }
}
