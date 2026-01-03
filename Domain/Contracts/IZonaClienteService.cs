using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Contracts
{
    public interface IZonaClienteService
    {
        Task<List<ClientesZona>> GetAllAsync();
        Task<ClientesZona?> GetByIdAsync(int id);
        Task CreateAsync(string nombre);
        Task UpdateAsync(int id, string nombre);
        Task DeleteAsync(int id);
    }

}
