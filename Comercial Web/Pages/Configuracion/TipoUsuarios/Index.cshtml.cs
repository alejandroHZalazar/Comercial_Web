using Domain.Contracts;
using Domain.Entities;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Comercial_Web.Pages.Configuracion.TipoUsuarios
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ITipoUsuarioService _service;
        private readonly IMenuPermisoService _menuPermisoService;


        public IndexModel(ITipoUsuarioService service, IMenuPermisoService menuPermisoService)

        {
            _service = service;
            _menuPermisoService = menuPermisoService;

        }

        // ====== Grilla ======
        public List<TipoUsuarioGridItem> Items { get; private set; } = new();

        // ====== Formulario ======
        [BindProperty] public int? Id { get; set; }
        [BindProperty] public string Descripcion { get; set; } = string.Empty;
        [BindProperty] public bool MostrarFormulario { get; set; } = false;
        [BindProperty] public List<int> PermisosSeleccionados { get; set; } = new();

        // ====== Permisos ======
        [BindProperty] public List<PermisoFormularioItem> PermisosFormulario { get; set; } = new();

        public async Task OnGetAsync()
        {
            await CargarGrillaAsync();
        }

        // Mostrar formulario vacío para nuevo
        public async Task<IActionResult> OnPostNuevoAsync()
        {
            MostrarFormulario = true;
            PermisosFormulario = await ObtenerPermisosDisponiblesAsync();
            await CargarGrillaAsync();
            return Page();
        }

        // Mostrar formulario con datos para editar
        public async Task<IActionResult> OnGetEditarAsync(int id)
        {
            var entity = await _service.GetByIdAsync(id);
            if (entity is null)
                return NotFound();

            Id = entity.Id;
            Descripcion = entity.Descripcion ?? string.Empty;

            var permisosSeleccionados = await _service.GetPermisosAsync(id);
            var disponibles = await ObtenerPermisosDisponiblesAsync();

            // marcar seleccionados
            foreach (var p in disponibles)
                p.Seleccionado = permisosSeleccionados.Contains(p.Id);

            PermisosFormulario = disponibles;
            MostrarFormulario = true;

            await CargarGrillaAsync();
            return Page();
        }

        // Guardar nuevo
        public async Task<IActionResult> OnPostGuardarNuevoAsync()
        {
            if (!FormularioValido())
            {
                MostrarFormulario = true;
               
                await CargarGrillaAsync();
                return Page();
            }

            
            var nuevoId = await _service.CreateAsync(Descripcion.Trim());
            

            
            await _service.SetPermisosAsync(nuevoId, PermisosSeleccionados);

            return RedirectToPage();
        }

        // Guardar edición
        public async Task<IActionResult> OnPostGuardarEdicionAsync()
        {
            if (!Id.HasValue || !FormularioValido())
            {
                MostrarFormulario = true;
                PermisosFormulario = await ObtenerPermisosDisponiblesAsync();
                await CargarGrillaAsync();
                return Page();
            }

            await _service.UpdateAsync(Id.Value, Descripcion.Trim());

            await _service.SetPermisosAsync(Id.Value, PermisosSeleccionados);

            return RedirectToPage();
        }

        // Eliminar
        public async Task<IActionResult> OnPostEliminarAsync(int id)
        {
            await _service.DeleteAsync(id);
            return RedirectToPage();
        }

        // Cancelar
        public async Task<IActionResult> OnPostCancelarAsync()
        {
            MostrarFormulario = false;
            await CargarGrillaAsync();
            return Page();
        }

        // ====== Helpers ======
        private async Task CargarGrillaAsync()
        {
            var tipos = await _service.GetAllAsync();
            Items = tipos.Select(t => new TipoUsuarioGridItem
            {
                Id = t.Id,
                Descripcion = t.Descripcion
            }).ToList();
        }

        private async Task<List<PermisoFormularioItem>> ObtenerPermisosDisponiblesAsync()
        {
            var disponibles = await _menuPermisoService.GetAllAsync();
            var permisos = disponibles.Select(m => new PermisoFormularioItem
            {
                Id = m.Id,
                Nombre = m.Funcion ?? string.Empty,
                Seleccionado = false
            }).ToList();

            if (Id.HasValue)
            {
                var seleccionados = await _menuPermisoService.GetPermisosPorTipoUsuarioAsync(Id.Value);
                foreach (var p in permisos)
                    p.Seleccionado = seleccionados.Contains(p.Id);
            }

            return permisos;

        }

        private bool FormularioValido()
        {
            if (string.IsNullOrWhiteSpace(Descripcion))
            {
                ModelState.AddModelError(nameof(Descripcion),
                    "Debe indicar un nombre para el tipo de Usuario");
                return false;
            }

            if (PermisosSeleccionados == null || !PermisosSeleccionados.Any())
            {
                ModelState.AddModelError("PermisosFormulario",
                    "Debe seleccionar al menos un permiso");
                return false;
            }

            return true;
        }
    }

    public class TipoUsuarioGridItem
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
    }

    public class PermisoFormularioItem
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Seleccionado { get; set; }
    }
}