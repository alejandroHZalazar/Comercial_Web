using Domain.Contracts;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class TipoUsuarioService : ITipoUsuarioService
{
    private readonly ComercialDbContext _context;

    public TipoUsuarioService(ComercialDbContext context)
    {
        _context = context;
    }

    public async Task<List<TipoUsuario>> GetAllAsync()
    {
        return await _context.TipoUsuarios.AsNoTracking().ToListAsync();
    }

    public async Task<TipoUsuario?> GetByIdAsync(int id)
    {
        return await _context.TipoUsuarios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<int> CreateAsync(string descripcion)
    {
        var entity = new TipoUsuario
        {
            Descripcion = descripcion
        };

        _context.TipoUsuarios.Add(entity);
        await _context.SaveChangesAsync();

        // EF Core completa la PK automáticamente
        return entity.Id;
    }

    public async Task UpdateAsync(int id, string descripcion)
    {
        var entity = await _context.TipoUsuarios.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) throw new Exception("Tipo de usuario no encontrado");

        entity.Descripcion = descripcion;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.TipoUsuarios.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) throw new Exception("Tipo de usuario no encontrado");

        _context.TipoUsuarios.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<List<int>> GetPermisosAsync(int tipoUsuarioId)
    {
        return await _context.TipoDeUsuariosPermisos
            .Where(x => x.FkTipoUsuario == tipoUsuarioId)
            .Select(x => x.FkMenuPermiso!.Value)
            .ToListAsync();
    }

    public async Task SetPermisosAsync(int tipoUsuarioId, List<int> permisos)
    {
        var existentes = _context.TipoDeUsuariosPermisos
            .Where(x => x.FkTipoUsuario == tipoUsuarioId);

        _context.TipoDeUsuariosPermisos.RemoveRange(existentes);

        var nuevos = permisos.Select(p => new TipoDeUsuariosPermiso
        {
            FkTipoUsuario = tipoUsuarioId,
            FkMenuPermiso = p
        });

        _context.TipoDeUsuariosPermisos.AddRange(nuevos);
        await _context.SaveChangesAsync();
    }
}