using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Comercial_Web.Pages.Configuracion.Parametros
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IParametroService _service;
        public IndexModel(IParametroService service) => _service = service;

        public List<Parametro> Items { get; private set; } = new();

        public async Task OnGetAsync()
        {
            Items = await _service.GetAllAsync();
        }

        // Handler AJAX para actualizar un parámetro inline
        public async Task<IActionResult> OnPostActualizarAsync([FromBody] ActualizarParametroRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Modulo) || string.IsNullOrWhiteSpace(request.Parametro))
                return BadRequest(new { error = "Módulo y parámetro son obligatorios" });

            await _service.ActualizarValorAsync(request.Modulo, request.Parametro, request.Valor ?? "");
            return new JsonResult(new { ok = true });
        }
    }

    public class ActualizarParametroRequest
    {
        public string Modulo { get; set; } = string.Empty;
        public string Parametro { get; set; } = string.Empty;
        public string? Valor { get; set; }
    }
}
