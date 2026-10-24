using Domain.DTO;
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

        /// <summary>Todos los rubros y subrubros con su ruta completa ("Rubro &gt; Subrubro"), ordenados por ruta.</summary>
        Task<List<RubroNodoDto>> GetAllConRutaAsync();

        Task<Rubro?> GetByIdAsync(int id);
        Task CreateAsync(string descripcion, int? fkRubroPadre = null);
        Task UpdateAsync(int id, string descripcion, int? fkRubroPadre = null);
        Task DeleteAsync(int id);
    }
}
