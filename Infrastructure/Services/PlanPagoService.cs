using Domain.Contracts;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class PlanPagoService : IPlanPagoService
    {
        private readonly ComercialDbContext _db;
        public PlanPagoService(ComercialDbContext db) => _db = db;

        public async Task<List<PlanPago>> GetByMedioPagoAsync(int medioPagoId)
        {
            return await _db.PlanesPago
                .Where(p => p.FkMedioPago == medioPagoId)
                .OrderBy(p => p.Nombre)
                .ToListAsync();
        }

        public async Task<PlanPago?> GetByIdAsync(int id)
        {
            return await _db.PlanesPago.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<PlanPago> CreateAsync(int fkMedioPago, string nombre, decimal? recargo)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("Debe indicar un nombre para el plan", nameof(nombre));

            var entity = new PlanPago
            {
                FkMedioPago = fkMedioPago,
                Nombre = nombre.Trim().ToUpperInvariant(),
                Recargo = recargo
            };

            _db.PlanesPago.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(int id, int fkMedioPago, string nombre, decimal? recargo)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("Debe indicar un nombre para el plan", nameof(nombre));

            var entity = await _db.PlanesPago.FindAsync(id)
                ?? throw new InvalidOperationException("Plan de pago no encontrado");

            entity.FkMedioPago = fkMedioPago;
            entity.Nombre = nombre.Trim().ToUpperInvariant();
            entity.Recargo = recargo;

            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _db.PlanesPago.FindAsync(id)
                ?? throw new InvalidOperationException("Plan de pago no encontrado");
            _db.PlanesPago.Remove(entity);
            await _db.SaveChangesAsync();
        }
    }
}
