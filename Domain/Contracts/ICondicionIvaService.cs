using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Contracts
{
    public interface ICondicionIvaService
    {
        Task<List<CondicionIva>> GetAllAsync();
        Task<CondicionIva?> GetByIdAsync(int id);
        Task<CondicionIva> CreateAsync(string descripcion, string abrev, string letra, string? abrevFE);
        Task UpdateAsync(int id, string descripcion, string abrev, string letra, string? abrevFE);
        Task DeleteAsync(int id);
    }

}
