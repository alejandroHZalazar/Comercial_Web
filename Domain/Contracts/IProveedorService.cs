using Domain.DTO;
using Domain.Entities;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IProveedorService
    {
        Task<List<Proveedore>> GetAllAsync();
        Task<Proveedore?> GetByIdAsync(int id);
        Task<int> CreateAsync(string nombreComercial, string? cuil, string? direccion, string? email, string? telefono, string? celular, decimal ganancia, decimal descuento);
        Task UpdateAsync(int id, string nombreComercial, string? cuil, string? direccion, string? email, string? telefono, string? celular, decimal ganancia, decimal descuento);
        Task DeleteAsync(int id); // baja lógica
        Task<List<ProveedorCabeceraDto>> TraerCabeceraAsync();

    }
}