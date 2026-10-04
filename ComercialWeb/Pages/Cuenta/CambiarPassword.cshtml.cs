using System.Security.Claims;
using Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Comercial_Web.Pages.Cuenta;

[Authorize]
public class CambiarPasswordModel : PageModel
{
    private const int LongitudMinima = 6;

    private readonly ILoginService _loginService;

    public CambiarPasswordModel(ILoginService loginService)
    {
        _loginService = loginService;
    }

    [BindProperty] public string PasswordActual       { get; set; } = "";
    [BindProperty] public string PasswordNueva        { get; set; } = "";
    [BindProperty] public string PasswordConfirmacion { get; set; } = "";

    public string? Error { get; private set; }
    public string? Exito { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrEmpty(PasswordActual))
            Error = "Ingresá tu contraseña actual.";
        else if (string.IsNullOrEmpty(PasswordNueva))
            Error = "Ingresá la nueva contraseña.";
        else if (PasswordNueva.Length < LongitudMinima)
            Error = $"La nueva contraseña debe tener al menos {LongitudMinima} caracteres.";
        else if (PasswordNueva != PasswordConfirmacion)
            Error = "La nueva contraseña y su confirmación no coinciden.";
        else if (PasswordNueva == PasswordActual)
            Error = "La nueva contraseña debe ser distinta de la actual.";

        if (Error == null)
        {
            var usuario = User.FindFirstValue(ClaimTypes.Name) ?? "";
            var (ok, error) = await _loginService.CambiarPasswordAsync(usuario, PasswordActual, PasswordNueva);
            if (ok) Exito = "La contraseña se actualizó correctamente.";
            else    Error = error;
        }

        // No se reenvían las contraseñas al formulario; la sesión actual se mantiene.
        PasswordActual = PasswordNueva = PasswordConfirmacion = "";
        return Page();
    }
}
