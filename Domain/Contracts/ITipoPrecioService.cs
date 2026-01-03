using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Contracts
{
    public interface ITipoPrecioService
    {
        Task<List<TipoPrecio>> GetAllAsync();
        Task<TipoPrecio?> GetByIdAsync(int id);

        Task<List<TipoValoresPrecio>> GetTiposValorAsync();

        Task CreateAsync(string descripcion, int tipoValorId, decimal valor);
        Task UpdateAsync(int id, string descripcion, int tipoValorId, decimal valor);
        Task DeleteAsync(int id);
    }
}
