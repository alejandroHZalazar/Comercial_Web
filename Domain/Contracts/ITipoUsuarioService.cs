using Domain.Entities;

namespace Domain.Contracts
{
    public interface ITipoUsuarioService
    {
        Task<List<TipoUsuario>> GetAllAsync();
        Task<TipoUsuario?> GetByIdAsync(int id);
        Task<int> CreateAsync(string descripcion);
        Task UpdateAsync(int id, string descripcion);
        Task DeleteAsync(int id);

        Task<List<int>> GetPermisosAsync(int tipoUsuarioId);
        Task SetPermisosAsync(int tipoUsuarioId, List<int> permisos);
    }
}