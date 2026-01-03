using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Contracts
{
    public interface IPorcentajeIvaService
    {
        Task<List<IvaPorcentaje>> GetAllAsync();
        Task<IvaPorcentaje?> GetByIdAsync(int id);
        Task CreateAsync(decimal valor);
        Task UpdateAsync(int id, decimal valor);
        Task DeleteAsync(int id);
    }
}
