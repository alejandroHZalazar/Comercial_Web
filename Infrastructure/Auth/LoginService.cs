using Application.Auth;
using Application.Security;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Auth;

public class LoginService : ILoginService
{
    private readonly ComercialDbContext _context;
    private readonly IPasswordService _passwordService;

    public LoginService(
        ComercialDbContext context,
        IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    public async Task<LoginResult> LoginAsync(string usuario, string password)
    {
        var user = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Nombre == usuario);

        if (user == null)
            return new(false, "Usuario o contraseña incorrectos", null);

        // 🔐 Password
        bool ok;

        if (_passwordService.IsHash(user.Password))
            ok = _passwordService.Verify(user.Password, password);
        else
            ok = user.Password == password;

        if (!ok)
            return new(false, "Usuario o contraseña incorrectos", null);

        // 🔄 Migración automática a hash
        if (!_passwordService.IsHash(user.Password))
        {
            user.Password = _passwordService.Hash(password);
            await _context.SaveChangesAsync();
        }

        // 🔍 Buscar nombre del tipo
        var tipoNombre = await _context.TipoUsuarios
            .Where(t => t.Id == user.Tipo)
            .Select(t => t.Descripcion)
            .FirstOrDefaultAsync();

        return new(true, null, tipoNombre);
    }





}
