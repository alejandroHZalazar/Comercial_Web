using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Comercial_Web.ViewModels.Configuracion;

    public class TipoPrecioViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe indicar una descripción")]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar un tipo de valor")]
        public int FkTipoValor { get; set; }

        [Required(ErrorMessage = "Debe indicar un valor")]
        public decimal Valor { get; set; }

        public List<SelectListItem> TiposValor { get; set; } = new();
    }

