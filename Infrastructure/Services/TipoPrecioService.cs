using Domain.Contracts;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;

public class TipoPrecioService : ITipoPrecioService
{
    private readonly ComercialDbContext _context;

    public TipoPrecioService(ComercialDbContext context)
    {
        _context = context;
    }

    public async Task<List<TipoPrecio>> GetAllAsync()
    {
        return await _context.TipoPrecios
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<TipoPrecio?> GetByIdAsync(int id)
    {
        return await _context.TipoPrecios
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<TipoValoresPrecio>> GetTiposValorAsync()
    {
        return await _context.TipoValoresPrecios
            .AsNoTracking()
            .OrderBy(x => x.Descripcion)
            .ToListAsync();
    }

    public async Task CreateAsync(string descripcion, int tipoValorId, decimal valor)
    {
        var entity = new TipoPrecio
        {
            Descripcion = descripcion,
            FkTipoValor = tipoValorId,
            Valor = valor
        };

        _context.TipoPrecios.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(int id, string descripcion, int tipoValorId, decimal valor)
    {
        var entity = await _context.TipoPrecios.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            throw new Exception("Tipo de precio no encontrado");

        entity.Descripcion = descripcion;
        entity.FkTipoValor = tipoValorId;
        entity.Valor = valor;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.TipoPrecios.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            throw new Exception("Tipo de precio no encontrado");

        _context.TipoPrecios.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
