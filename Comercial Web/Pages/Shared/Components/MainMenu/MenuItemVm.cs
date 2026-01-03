using System;

namespace Comercial_Web.Pages.Shared.Components.MainMenu
{
    public class MenuItemVm
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? Url { get; set; }

        public string? Icono { get; set; } = "generic.png";

        public List<MenuItemVm> Hijos { get; set; } = new();
    }
}
