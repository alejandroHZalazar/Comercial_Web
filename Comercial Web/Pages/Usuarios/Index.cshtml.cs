using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Comercial_Web.Pages.Usuarios
{
    public class IndexModel : PageModel
    {
        private readonly IUsuarioService _usuarioService;
        private readonly ITipoUsuarioService _tipoUsuarioService;

        public IndexModel(IUsuarioService usuarioService, ITipoUsuarioService tipoUsuarioService)
        {
            _usuarioService = usuarioService;
            _tipoUsuarioService = tipoUsuarioService;
        }

        public List<UsuarioGridItem> Items { get; private set; } = new();
        public List<SelectListItem> TiposUsuario { get; private set; } = new();

        [BindProperty] public int? Id { get; set; }
        [BindProperty] public string Nombre { get; set; } = string.Empty;
        [BindProperty] public int? TipoUsuarioId { get; set; }
        [BindProperty] public string Contraseña { get; set; } = string.Empty;
        [BindProperty] public string RepitaContraseña { get; set; } = string.Empty;
        [BindProperty] public bool MostrarFormulario { get; set; } = false;

        public async Task OnGetAsync() => await CargarGrillaAsync();

        public async Task<IActionResult> OnPostNuevoAsync()
        {
            MostrarFormulario = true;
            Contraseña = ""; RepitaContraseña = ""; // limpio en alta
            await CargarTiposAsync();
            await CargarGrillaAsync();
            return Page();
        }

        public async Task<IActionResult> OnGetEditarAsync(int id)
        {
            var usuario = await _usuarioService.GetByIdAsync(id);
            if (usuario is null) return NotFound();

            Id = usuario.Id;
            Nombre = usuario.Nombre ?? "";
            TipoUsuarioId = usuario.Tipo;
            Contraseña = "";              // en edición no mostramos hash ni password actual
            RepitaContraseña = "";

            MostrarFormulario = true;
            await CargarTiposAsync();
            await CargarGrillaAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostGuardarNuevoAsync()
        {
            if (!FormularioValido(isEdit: false))
            {
                MostrarFormulario = true;
                await CargarTiposAsync();
                await CargarGrillaAsync();
                return Page();
            }

            await _usuarioService.CreateAsync(Nombre.Trim(), Contraseña.Trim(), TipoUsuarioId!.Value);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostGuardarEdicionAsync()
        {
            if (!Id.HasValue || !FormularioValido(isEdit: true))
            {
                MostrarFormulario = true;
                await CargarTiposAsync();
                await CargarGrillaAsync();
                return Page();
            }

            await _usuarioService.UpdateAsync(
                Id.Value,
                Nombre.Trim(),
                TipoUsuarioId!.Value,
                string.IsNullOrWhiteSpace(Contraseña) ? null : Contraseña.Trim()
            );

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEliminarAsync(int id)
        {
            await _usuarioService.DeleteAsync(id);
            return RedirectToPage();
        }

        private async Task CargarGrillaAsync()
        {
            var usuarios = await _usuarioService.GetAllAsync();
            // Si necesitás la descripción del tipo, podés resolverla con un join o servicio de tipos.
            Items = usuarios.Select(u => new UsuarioGridItem
            {
                Id = u.Id,
                Nombre = u.Nombre ?? "",
                TipoDescripcion = u.Tipo?.ToString() ?? "" // reemplazar por descripción real si la tenés
            }).ToList();
        }

        private async Task CargarTiposAsync()
        {
            var tipos = await _tipoUsuarioService.GetAllAsync();
            TiposUsuario = tipos.Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.Descripcion
            }).ToList();
        }

        private bool FormularioValido(bool isEdit)
        {
            if (string.IsNullOrWhiteSpace(Nombre))
            {
                ModelState.AddModelError(nameof(Nombre), "Debe escribir un nombre");
                return false;
            }

            if (!TipoUsuarioId.HasValue)
            {
                ModelState.AddModelError(nameof(TipoUsuarioId), "Debe seleccionar un tipo de usuario");
                return false;
            }

            if (!isEdit) // Alta: validar password obligatorio
            {
                if (string.IsNullOrWhiteSpace(Contraseña))
                {
                    ModelState.AddModelError(nameof(Contraseña), "Debe escribir una contraseña");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(RepitaContraseña))
                {
                    ModelState.AddModelError(nameof(RepitaContraseña), "Debe repetir la contraseña");
                    return false;
                }

                if (!string.Equals(Contraseña, RepitaContraseña))
                {
                    ModelState.AddModelError(nameof(RepitaContraseña), "Las contraseñas deben coincidir");
                    return false;
                }
            }
            else // Edición: password opcional, pero si viene, validar coincidencia
            {
                var vienePassword = !string.IsNullOrWhiteSpace(Contraseña) || !string.IsNullOrWhiteSpace(RepitaContraseña);
                if (vienePassword)
                {
                    if (string.IsNullOrWhiteSpace(Contraseña) || string.IsNullOrWhiteSpace(RepitaContraseña))
                    {
                        ModelState.AddModelError(nameof(RepitaContraseña), "Debe completar ambos campos de contraseña");
                        return false;
                    }

                    if (!string.Equals(Contraseña, RepitaContraseña))
                    {
                        ModelState.AddModelError(nameof(RepitaContraseña), "Las contraseñas deben coincidir");
                        return false;
                    }
                }
            }

            return true;
        }

        public class UsuarioGridItem
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string TipoDescripcion { get; set; } = string.Empty;
        }

        public class TipoUsuarioItem
        {
            public int Id { get; set; }
            public string Descripcion { get; set; } = string.Empty;
        }
    }
}