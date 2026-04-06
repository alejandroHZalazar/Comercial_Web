using Domain.Contracts;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class ConceptoCajaService : IConceptoCajaService
    {
        private readonly ComercialDbContext _db;
        public ConceptoCajaService(ComercialDbContext db) => _db = db;

        public async Task<List<ConceptoCaja>> GetAllAsync()
        {
            return await _db.ConceptosCaja
                .OrderBy(c => c.Id)
                .ToListAsync();
        }

        public async Task<ConceptoCaja?> GetByIdAsync(int id)
        {
            return await _db.ConceptosCaja.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<ConceptoCaja> CreateAsync(string nombre, string tipoMovimiento, bool afectaEfectivo, int? fkMedioPago, string? operacion)
        {
            Validar(nombre, tipoMovimiento);

            var entity = new ConceptoCaja
            {
                Nombre = nombre.Trim().ToUpperInvariant(),
                TipoMovimiento = tipoMovimiento.Trim().ToUpperInvariant(),
                AfectaEfectivo = afectaEfectivo,
                FkMedioPago = fkMedioPago,
                Operacion = operacion
            };

            _db.ConceptosCaja.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(int id, string nombre, string tipoMovimiento, bool afectaEfectivo, int? fkMedioPago, string? operacion)
        {
            Validar(nombre, tipoMovimiento);

            var entity = await _db.ConceptosCaja.FindAsync(id)
                ?? throw new InvalidOperationException("Registro no encontrado");

            entity.Nombre = nombre.Trim().ToUpperInvariant();
            entity.TipoMovimiento = tipoMovimiento.Trim().ToUpperInvariant();
            entity.AfectaEfectivo = afectaEfectivo;
            entity.FkMedioPago = fkMedioPago;
            entity.Operacion = operacion;

            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _db.ConceptosCaja.FindAsync(id)
                ?? throw new InvalidOperationException("Registro no encontrado");
            _db.ConceptosCaja.Remove(entity);
            await _db.SaveChangesAsync();
        }

        private static void Validar(string nombre, string tipoMovimiento)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("Debe escribir un nombre", nameof(nombre));
            if (tipoMovimiento != "I" && tipoMovimiento != "E")
                throw new ArgumentException("El tipo de movimiento debe ser I (Ingreso) o E (Egreso)", nameof(tipoMovimiento));
        }
    }
}
