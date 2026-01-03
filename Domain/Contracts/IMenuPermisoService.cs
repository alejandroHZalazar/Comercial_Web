using Domain.Entities;

namespace Domain.Contracts
{
    public interface IMenuPermisoService
    {
        Task<List<MenuPermiso>> GetAllAsync();
        Task<List<int>> GetPermisosPorTipoUsuarioAsync(int tipoUsuarioId);
    }

}
