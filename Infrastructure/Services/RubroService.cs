using Domain.Contracts;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class RubroService : IRubroService
{
    private readonly ComercialDbContext _db;
    public RubroService(ComercialDbContext db) => _db = db;

    public async Task<List<Rubro>> GetAllAsync() =>
        await _db.Rubros
            .OrderBy(r => r.Descripcion)
            .ToListAsync();

    public async Task<Rubro?> GetByIdAsync(int id) =>
        await _db.Rubros.FindAsync(id);

    public async Task CreateAsync(string descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ArgumentException("Debe indicar una descripción");

        _db.Rubros.Add(new Rubro
        {
            Descripcion = descripcion.Trim().ToUpper()
        });

        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(int id, string descripcion)
    {
        var rubro = await _db.Rubros.FindAsync(id)
            ?? throw new InvalidOperationException("Rubro no encontrado");

        rubro.Descripcion = descripcion.Trim().ToUpper();
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var rubro = await _db.Rubros.FindAsync(id)
            ?? throw new InvalidOperationException("Rubro no encontrado");

        _db.Rubros.Remove(rubro);
        await _db.SaveChangesAsync();
    }
}
