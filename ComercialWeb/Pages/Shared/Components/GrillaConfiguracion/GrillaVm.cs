namespace Comercial_Web.Pages.Shared.Components.GrillaConfiguracion
{
    public class GrillaVm
    {
        public List<string> Columnas { get; set; } = new();
        public List<FilaVm> Filas { get; set; } = new();
    }

    public class FilaVm
    {
        public int Id { get; set; }
        public List<string> Valores { get; set; } = new();
    }
}