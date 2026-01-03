using Application.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Comercial_Web.Pages;

public class LoginModel : PageModel
{
    private readonly ILoginService _loginService;

    public LoginModel(ILoginService loginService)
    {
        _loginService = loginService;
    }

    [BindProperty]
    public string Usuario { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    public string? Error { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await _loginService.LoginAsync(Usuario, Password);

        if (!result.Success)
        {
            Error = result.Error;
            return Page();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, Usuario),
            new Claim("TipoUsuario", result.TipoUsuario!)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);

        return RedirectToPage("/Index");
    }
}
