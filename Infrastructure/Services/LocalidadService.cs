using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;

public class LocalidadService : ILocalidadService
{
    private readonly ComercialDbContext _context;

    public LocalidadService(ComercialDbContext context)
    {
        _context = context;
    }

    public async Task<List<Provincia>> GetProvinciasAsync()
    {
        return await _context.Provincias
            .Where(p => p.Baja != true)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    public async Task<List<Localidade>> GetByProvinciaAsync(int? provinciaId)
    {
        var query = _context.Localidades
            .Where(l => l.Baja != true);

        if (provinciaId.HasValue)
            query = query.Where(l => l.FkProvincia == provinciaId);

        return await query.OrderBy(l => l.Nombre).ToListAsync();
    }

    public async Task<Localidade?> GetByIdAsync(int id)
    {
        return await _context.Localidades
            .FirstOrDefaultAsync(l => l.Id == id && l.Baja != true);
    }

    public async Task CreateAsync(Localidade loc)
    {
        _context.Localidades.Add(loc);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Localidade loc)
    {
        var existente = await _context.Localidades.FindAsync(loc.Id);
        if (existente is null) return;

        existente.Nombre = loc.Nombre;
        existente.FkProvincia = loc.FkProvincia;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var loc = await _context.Localidades.FindAsync(id);
        if (loc is null) return;

        loc.Baja = true;
        await _context.SaveChangesAsync();
    }
}