using Domain.Entities;

namespace Application.Interfaces
{
    public interface IClienteService
    {
        Task<List<Cliente>> GetAllAsync();
        Task<Cliente?> GetByIdAsync(int id);
        Task<int> CreateAsync(string nombreComercial);
        Task UpdateAsync(int id, string nombreComercial);
        Task DeleteAsync(int id); // baja lógica
    }
}
