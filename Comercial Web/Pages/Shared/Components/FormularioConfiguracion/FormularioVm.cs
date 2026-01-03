namespace Comercial_Web.Pages.Shared.Components.FormularioConfiguracion
{
    // Se agrega un nuevo tipo de campo: CheckboxList
    public enum TipoCampo { Texto, Select, Textarea, Number, CheckboxList, Password }

    public class FormularioVm
    {
        public string TituloPrincipal { get; set; } = string.Empty;
        public string TituloSecundario { get; set; } = string.Empty;
        public string Handler { get; set; } = string.Empty;
        public string UrlCancelar { get; set; } = string.Empty;
        public bool MostrarFormulario { get; set; } = false;

        public int? Id { get; set; }
        public List<CampoVm> Campos { get; set; } = new();
        public List<CheckboxItemVm> PermisosFormulario { get; set; } = new();

    }

    public class CampoVm
    {
        public string Nombre { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
        public TipoCampo Tipo { get; set; } = TipoCampo.Texto;

        // Para selects
        public IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>? Opciones { get; set; }

        // Para listas de checkboxes
        public List<CheckboxItemVm>? Items { get; set; }

        public string? MensajeError { get; set; }
    }

    // Nuevo modelo para ítems de checkbox
    public class CheckboxItemVm
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Seleccionado { get; set; }
    }
}