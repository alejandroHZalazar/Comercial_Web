using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Domain.DTO.UsuarioDTO;

namespace Domain.Contracts
{
    public interface IUsuarioService
    {
        Task<List<Usuario>> GetAllAsync();
        Task<Usuario?> GetByIdAsync(int id);
        Task<int> CreateAsync(string nombre, string password, int tipoUsuarioId);
        Task UpdateAsync(int id, string nombre, int tipoUsuarioId, string? password = null);
        Task DeleteAsync(int id);
        Task<List<UsuarioGridABMItem>> GetUsuarioGrillaABM();

    }

}
