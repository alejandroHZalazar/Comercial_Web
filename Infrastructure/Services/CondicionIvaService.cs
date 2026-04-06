// Infrastructure/Services/CondicionIvaService.cs
using Domain.Contracts;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class CondicionIvaService : ICondicionIvaService
    {
        private readonly ComercialDbContext _db;
        public CondicionIvaService(ComercialDbContext db) => _db = db;

        public async Task<List<CondicionIva>> GetAllAsync()
        {
            return await _db.CondIvas
                .OrderBy(c => c.Descripcion) // similar a grilla ordenada
                .ToListAsync();
        }

        public async Task<CondicionIva?> GetByIdAsync(int id)
        {
            return await _db.CondIvas.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<CondicionIva> CreateAsync(string descripcion, string abrev, string letra, string? abrevFE)
        {
            Validar(descripcion, abrev, letra);
            Upper(ref descripcion, ref abrev, ref letra);

            var entity = new CondicionIva { Descripcion = descripcion, Abrev = abrev, Letra = letra, AbrevFE = abrevFE?.Trim().ToUpperInvariant() };
            _db.CondIvas.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(int id, string descripcion, string abrev, string letra, string? abrevFE)
        {
            Validar(descripcion, abrev, letra);
            Upper(ref descripcion, ref abrev, ref letra);

            var entity = await _db.CondIvas.FindAsync(id)
                ?? throw new InvalidOperationException("Registro no encontrado");

            entity.Descripcion = descripcion;
            entity.Abrev = abrev;
            entity.Letra = letra;
            entity.AbrevFE = abrevFE?.Trim().ToUpperInvariant();
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _db.CondIvas.FindAsync(id)
                ?? throw new InvalidOperationException("Registro no encontrado");
            _db.CondIvas.Remove(entity);
            await _db.SaveChangesAsync();
        }

        private static void Validar(string descripcion, string abrev, string letra)
        {
            if (string.IsNullOrWhiteSpace(descripcion)) throw new ArgumentException("Debe escribir una descripción", nameof(descripcion));
            if (string.IsNullOrWhiteSpace(abrev)) throw new ArgumentException("Debe escribir una Abreviatura", nameof(abrev));
            if (string.IsNullOrWhiteSpace(letra)) throw new ArgumentException("Indique una letra", nameof(letra));
        }

        private static void Upper(ref string d, ref string a, ref string l)
        {
            d = d.Trim().ToUpperInvariant();
            a = a.Trim().ToUpperInvariant();
            l = l.Trim().ToUpperInvariant();
        }
    }
}