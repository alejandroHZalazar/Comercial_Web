using Domain.Entities;

namespace Domain.Contracts
{
    public interface IMedioPagoService
    {
        Task<List<MedioPago>> GetAllAsync();
        Task<MedioPago?> GetByIdAsync(int id);
        Task<MedioPago> CreateAsync(string nombre, decimal? recargo, bool? conDatos);
        Task UpdateAsync(int id, string nombre, decimal? recargo, bool? conDatos);
        Task DeleteAsync(int id);
    }
}
