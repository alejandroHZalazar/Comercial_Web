using Domain.Contracts;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class DocumentoTipoService : IDocumentoTipoService
    {
        private readonly ComercialDbContext _db;
        public DocumentoTipoService(ComercialDbContext db) => _db = db;

        public async Task<List<DocumentoTipo>> GetAllAsync()
        {
            return await _db.DocumentosTipo
                .OrderBy(d => d.Id)
                .ToListAsync();
        }

        public async Task<DocumentoTipo?> GetByIdAsync(int id)
        {
            return await _db.DocumentosTipo.FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<DocumentoTipo> CreateAsync(string abreviatura, string? descripcion)
        {
            Validar(abreviatura);

            var entity = new DocumentoTipo
            {
                Abreviatura = abreviatura.Trim().ToUpperInvariant(),
                Descripcion = descripcion?.Trim().ToUpperInvariant()
            };

            _db.DocumentosTipo.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(int id, string abreviatura, string? descripcion)
        {
            Validar(abreviatura);

            var entity = await _db.DocumentosTipo.FindAsync(id)
                ?? throw new InvalidOperationException("Registro no encontrado");

            entity.Abreviatura = abreviatura.Trim().ToUpperInvariant();
            entity.Descripcion = descripcion?.Trim().ToUpperInvariant();

            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _db.DocumentosTipo.FindAsync(id)
                ?? throw new InvalidOperationException("Registro no encontrado");
            _db.DocumentosTipo.Remove(entity);
            await _db.SaveChangesAsync();
        }

        private static void Validar(string abreviatura)
        {
            if (string.IsNullOrWhiteSpace(abreviatura))
                throw new ArgumentException("Debe indicar una abreviatura", nameof(abreviatura));
            if (abreviatura.Trim().Length > 2)
                throw new ArgumentException("La abreviatura no puede superar 2 caracteres", nameof(abreviatura));
        }
    }
}
