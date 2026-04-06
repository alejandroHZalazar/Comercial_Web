using Domain.Contracts;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class MedioPagoService : IMedioPagoService
    {
        private readonly ComercialDbContext _db;
        public MedioPagoService(ComercialDbContext db) => _db = db;

        public async Task<List<MedioPago>> GetAllAsync()
        {
            return await _db.MediosPago
                .OrderBy(m => m.Nombre)
                .ToListAsync();
        }

        public async Task<MedioPago?> GetByIdAsync(int id)
        {
            return await _db.MediosPago.FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<MedioPago> CreateAsync(string nombre, decimal? recargo, bool? conDatos)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("Debe indicar un nombre", nameof(nombre));

            var entity = new MedioPago
            {
                Nombre = nombre.Trim().ToUpperInvariant(),
                Recargo = recargo,
                ConDatos = conDatos
            };

            _db.MediosPago.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(int id, string nombre, decimal? recargo, bool? conDatos)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("Debe indicar un nombre", nameof(nombre));

            var entity = await _db.MediosPago.FindAsync(id)
                ?? throw new InvalidOperationException("Registro no encontrado");

            entity.Nombre = nombre.Trim().ToUpperInvariant();
            entity.Recargo = recargo;
            entity.ConDatos = conDatos;

            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _db.MediosPago.FindAsync(id)
                ?? throw new InvalidOperationException("Registro no encontrado");
            _db.MediosPago.Remove(entity);
            await _db.SaveChangesAsync();
        }
    }
}
