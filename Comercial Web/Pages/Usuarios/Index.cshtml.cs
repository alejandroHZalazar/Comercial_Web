using Domain.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using static Domain.DTO.UsuarioDTO;

namespace Comercial_Web.Pages.Usuarios;

[Authorize]
[ValidateAntiForgeryToken]
public class IndexModel : PageModel
{
    private readonly IUsuarioService     _usuarioService;
    private readonly ITipoUsuarioService _tipoUsuarioService;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public IndexModel(IUsuarioService usuarioService, ITipoUsuarioService tipoUsuarioService)
    {
        _usuarioService     = usuarioService;
        _tipoUsuarioService = tipoUsuarioService;
    }

    // Lista para renderizado inicial en SSR (tabla)
    public List<UsuarioGridABMItem> Items { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Items = await _usuarioService.GetUsuarioGrillaABM();
    }

    // ----------------------------------------------------------------
    //  GET: lista de tipos de usuario para el dropdown del modal
    // ----------------------------------------------------------------
    public async Task<JsonResult> OnGetTiposAsync()
    {
        var tipos = (await _tipoUsuarioService.GetAllAsync())
            .Select(t => new { id = t.Id, descripcion = t.Descripcion })
            .ToList();

        return new JsonResult(tipos, JsonOpts);
    }

    // ----------------------------------------------------------------
    //  GET: datos de un usuario para pre-cargar el modal de edición
    // ----------------------------------------------------------------
    public async Task<JsonResult> OnGetEditarAsync(int id)
    {
        var u = await _usuarioService.GetByIdAsync(id);
        if (u is null)
            return new JsonResult(new { error = "Usuario no encontrado" }, JsonOpts) { StatusCode = 404 };

        return new JsonResult(new
        {
            id     = u.Id,
            nombre = u.Nombre ?? "",
            tipo   = u.Tipo
        }, JsonOpts);
    }

    // ----------------------------------------------------------------
    //  POST: crear o actualizar usuario
    // ----------------------------------------------------------------
    public async Task<JsonResult> OnPostGuardarAsync([FromBody] GuardarRequest req)
    {
        try
        {
            if (req.Id.HasValue && req.Id.Value > 0)
                await _usuarioService.UpdateAsync(
                    req.Id.Value,
                    req.Nombre.Trim(),
                    req.TipoUsuarioId,
                    string.IsNullOrWhiteSpace(req.Password) ? null : req.Password.Trim());
            else
                await _usuarioService.CreateAsync(req.Nombre.Trim(), req.Password!.Trim(), req.TipoUsuarioId);

            return new JsonResult(new { ok = true }, JsonOpts);
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = ex.Message }, JsonOpts) { StatusCode = 500 };
        }
    }

    // ----------------------------------------------------------------
    //  POST: baja lógica
    // ----------------------------------------------------------------
    public async Task<JsonResult> OnPostEliminarAsync([FromBody] IdRequest req)
    {
        try
        {
            await _usuarioService.DeleteAsync(req.Id);
            return new JsonResult(new { ok = true }, JsonOpts);
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = ex.Message }, JsonOpts) { StatusCode = 500 };
        }
    }

    // ----------------------------------------------------------------
    //  Request models
    // ----------------------------------------------------------------
    public class GuardarRequest
    {
        public int?   Id           { get; set; }
        public string Nombre       { get; set; } = string.Empty;
        public int    TipoUsuarioId { get; set; }
        public string? Password    { get; set; }
        public string? Password2   { get; set; }
    }

    public class IdRequest
    {
        public int Id { get; set; }
    }
}
