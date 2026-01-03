using Domain.Contracts;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;
public class PorcentajeIvaService : IPorcentajeIvaService
{
    private readonly ComercialDbContext _db;
    public PorcentajeIvaService(ComercialDbContext db) => _db = db;

    public async Task<List<IvaPorcentaje>> GetAllAsync() =>
        await _db.IvaPorcentajes.OrderBy(p => p.Valor).ToListAsync();

    public async Task<IvaPorcentaje?> GetByIdAsync(int id) =>
        await _db.IvaPorcentajes.FindAsync(id);

    public async Task CreateAsync(decimal valor)
    {
        if (valor <= 0) throw new ArgumentException("Debe indicar un porcentaje válido");
        _db.IvaPorcentajes.Add(new IvaPorcentaje { Valor = valor });
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(int id, decimal valor)
    {
        var entity = await _db.IvaPorcentajes.FindAsync(id)
            ?? throw new InvalidOperationException("Registro no encontrado");
        entity.Valor = valor;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _db.IvaPorcentajes.FindAsync(id)
            ?? throw new InvalidOperationException("Registro no encontrado");
        _db.IvaPorcentajes.Remove(entity);
        await _db.SaveChangesAsync();
    }
}