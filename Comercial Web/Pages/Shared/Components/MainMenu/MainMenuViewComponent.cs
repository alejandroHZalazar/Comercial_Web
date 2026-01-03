using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Comercial_Web.Pages.Shared.Components.MainMenu
{
    public class MainMenuViewComponent : ViewComponent
    {
        private readonly ComercialDbContext _context;

        public MainMenuViewComponent(ComercialDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var tipoUsuario = UserClaimsPrincipal.FindFirst("TipoUsuario")?.Value;
            if (tipoUsuario == null)
                return View(new List<MenuItemVm>());
            var permisos = await (
                from m in _context.MenuPermisos
                join tp in _context.TipoDeUsuariosPermisos
                    on m.Id equals tp.FkMenuPermiso
                join tu in _context.TipoUsuarios
                    on tp.FkTipoUsuario equals tu.Id
                where tu.Descripcion == tipoUsuario
                select new MenuItemVm
                {
                    Id = m.Id,
                    Descripcion = m.Funcion,
                    Nombre = m.NombreControl,
                    Url = m.Url
                }
            ).ToListAsync();

            var padres = permisos
             .Where(x => x.Id % 100 == 0 || x.Id == 1) // incluir explícitamente el menú configuración
             .OrderBy(x => x.Id)
             .ToList();

            foreach (var padre in padres)
            {
                padre.Hijos = permisos
                    .Where(x => x.Id / 100 == padre.Id / 100 && x.Id != padre.Id)
                    .OrderBy(x => x.Id)
                    .ToList();
            }


            return View(padres);
        }
    }
}
