using Domain.Entities;

namespace Domain.Contracts
{
    public interface IConceptoCajaService
    {
        Task<List<ConceptoCaja>> GetAllAsync();
        Task<ConceptoCaja?> GetByIdAsync(int id);
        Task<ConceptoCaja> CreateAsync(string nombre, string tipoMovimiento, bool afectaEfectivo, int? fkMedioPago, string? operacion);
        Task UpdateAsync(int id, string nombre, string tipoMovimiento, bool afectaEfectivo, int? fkMedioPago, string? operacion);
        Task DeleteAsync(int id);
    }
}
