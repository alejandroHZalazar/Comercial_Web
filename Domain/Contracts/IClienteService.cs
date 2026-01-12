using Domain.Entities;
using System.Threading.Tasks;
using static Domain.DTO.ClienteDTO;

namespace Application.Interfaces
{
    public interface IClienteService
    {
        Task<List<Cliente>> GetAllAsync();
        Task<Cliente?> GetByIdAsync(int id);
        Task<int> CreateAsync(Cliente unCliente);
        Task UpdateAsync(Cliente unCliente);
        Task DeleteAsync(int id); // baja lógica
        Task<List<Cliente>> BuscarAsync(int tipoBusqueda, string valor);
        Task<List<Cliente>> BuscarPorLocalidadAsync(string valor);
        Task<List<Cliente>> BuscarPorZonaAsync(string valor);
        Task<ClienteDetalleDTO> traerDetalleAsync(int id);
    }
}
