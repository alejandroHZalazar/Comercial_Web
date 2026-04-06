using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Comercial_Web.Pages.Configuracion.MediosPago
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IMedioPagoService _service;
        private readonly IPlanPagoService _planPagoService;

        public IndexModel(IMedioPagoService service, IPlanPagoService planPagoService)
        {
            _service = service;
            _planPagoService = planPagoService;
        }

        // ====== Grilla Medios de Pago ======
        public List<MedioPago> Items { get; private set; } = new();

        // ====== Modal Medio de Pago ======
        [BindProperty] public int? Id { get; set; }
        [BindProperty] public string Nombre { get; set; } = string.Empty;
        [BindProperty] public decimal? Recargo { get; set; }
        [BindProperty] public bool NecesitaDatos { get; set; }
        public bool MostrarModal { get; set; }

        // ====== Modal Planes de Pago ======
        [BindProperty] public int? PlanId { get; set; }
        [BindProperty] public string PlanNombre { get; set; } = string.Empty;
        [BindProperty] public decimal? PlanRecargo { get; set; }
        [BindProperty] public int? PlanMedioPagoId { get; set; }
        public string? PlanMedioPagoNombre { get; set; }
        public List<PlanPago> Planes { get; private set; } = new();
        public bool MostrarModalPlanes { get; set; }
        public bool MostrarFormPlan { get; set; }

        // =====================

        public async Task OnGetAsync()
        {
            await CargarGrillaAsync();
        }

        // ---- MEDIO DE PAGO: Nuevo ----
        public async Task<IActionResult> OnPostNuevoAsync()
        {
            MostrarModal = true;
            Id = null;
            Nombre = string.Empty;
            Recargo = null;
            NecesitaDatos = false;

            await CargarGrillaAsync();
            return Page();
        }

        // ---- MEDIO DE PAGO: Editar (GET) ----
        public async Task<IActionResult> OnGetEditarAsync(int id)
        {
            await CargarGrillaAsync();

            var item = await _service.GetByIdAsync(id);
            if (item is null) return NotFound();

            Id = item.Id;
            Nombre = item.Nombre;
            Recargo = item.Recargo;
            NecesitaDatos = item.ConDatos ?? false;

            MostrarModal = true;
            return Page();
        }

        // ---- MEDIO DE PAGO: Agregar (POST) ----
        public async Task<IActionResult> OnPostAgregarAsync()
        {
            await _service.CreateAsync(Nombre, Recargo, NecesitaDatos);
            return RedirectToPage();
        }

        // ---- MEDIO DE PAGO: Guardar edición (POST) ----
        public async Task<IActionResult> OnPostEditarAsync()
        {
            if (!Id.HasValue) return BadRequest();
            await _service.UpdateAsync(Id.Value, Nombre, Recargo, NecesitaDatos);
            return RedirectToPage();
        }

        // ---- MEDIO DE PAGO: Eliminar (POST) ----
        public async Task<IActionResult> OnPostEliminarAsync(int id)
        {
            await _service.DeleteAsync(id);
            return RedirectToPage();
        }

        // ============ PLANES DE PAGO ============

        // ---- Abrir modal planes (GET) ----
        public async Task<IActionResult> OnGetPlanesAsync(int id)
        {
            await CargarGrillaAsync();

            var medio = await _service.GetByIdAsync(id);
            if (medio is null) return NotFound();

            PlanMedioPagoId = medio.Id;
            PlanMedioPagoNombre = medio.Nombre;
            Planes = await _planPagoService.GetByMedioPagoAsync(medio.Id);

            MostrarModalPlanes = true;
            return Page();
        }

        // ---- Plan: Nuevo (POST) ----
        public async Task<IActionResult> OnPostPlanNuevoAsync()
        {
            await CargarGrillaAsync();

            if (PlanMedioPagoId.HasValue)
            {
                var medio = await _service.GetByIdAsync(PlanMedioPagoId.Value);
                PlanMedioPagoNombre = medio?.Nombre;
                Planes = await _planPagoService.GetByMedioPagoAsync(PlanMedioPagoId.Value);
            }

            PlanId = null;
            PlanNombre = string.Empty;
            PlanRecargo = null;
            MostrarModalPlanes = true;
            MostrarFormPlan = true;

            return Page();
        }

        // ---- Plan: Editar (POST) ----
        public async Task<IActionResult> OnPostPlanEditarFormAsync(int planId)
        {
            await CargarGrillaAsync();

            var plan = await _planPagoService.GetByIdAsync(planId);
            if (plan is null) return NotFound();

            PlanId = plan.Id;
            PlanNombre = plan.Nombre;
            PlanRecargo = plan.Recargo;
            PlanMedioPagoId = plan.FkMedioPago;

            var medio = plan.FkMedioPago.HasValue ? await _service.GetByIdAsync(plan.FkMedioPago.Value) : null;
            PlanMedioPagoNombre = medio?.Nombre;
            Planes = plan.FkMedioPago.HasValue
                ? await _planPagoService.GetByMedioPagoAsync(plan.FkMedioPago.Value)
                : new();

            MostrarModalPlanes = true;
            MostrarFormPlan = true;

            return Page();
        }

        // ---- Plan: Guardar (POST) ----
        public async Task<IActionResult> OnPostPlanGuardarAsync()
        {
            if (!PlanMedioPagoId.HasValue) return BadRequest();

            if (PlanId.HasValue)
                await _planPagoService.UpdateAsync(PlanId.Value, PlanMedioPagoId.Value, PlanNombre, PlanRecargo);
            else
                await _planPagoService.CreateAsync(PlanMedioPagoId.Value, PlanNombre, PlanRecargo);

            // Recargar modal de planes
            return RedirectToPage("Index", "Planes", new { id = PlanMedioPagoId.Value });
        }

        // ---- Plan: Eliminar (POST) ----
        public async Task<IActionResult> OnPostPlanEliminarAsync(int planId, int medioPagoId)
        {
            await _planPagoService.DeleteAsync(planId);
            return RedirectToPage("Index", "Planes", new { id = medioPagoId });
        }

        // ====== Helpers ======

        private async Task CargarGrillaAsync()
        {
            Items = await _service.GetAllAsync();
        }
    }
}
