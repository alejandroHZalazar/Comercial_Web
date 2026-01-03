using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Contracts
{
    public interface IRubroService
    {
        Task<List<Rubro>> GetAllAsync();
        Task<Rubro?> GetByIdAsync(int id);
        Task CreateAsync(string descripcion);
        Task UpdateAsync(int id, string descripcion);
        Task DeleteAsync(int id);
    }
}
