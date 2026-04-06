using Domain.Entities;

namespace Domain.Contracts
{
    public interface IPlanPagoService
    {
        Task<List<PlanPago>> GetByMedioPagoAsync(int medioPagoId);
        Task<PlanPago?> GetByIdAsync(int id);
        Task<PlanPago> CreateAsync(int fkMedioPago, string nombre, decimal? recargo);
        Task UpdateAsync(int id, int fkMedioPago, string nombre, decimal? recargo);
        Task DeleteAsync(int id);
    }
}
