using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Contracts
{
    public interface IParametroService
    {
        Task<List<Parametro>> GetAllAsync();
        Task<Parametro?> GetLogoAsync();
        Task UpdateLogoAsync(string nombreArchivo, byte[] imagen);
        Task<int> ObtenerIndiceBusquedaNotaPedidoAsync();
        Task<int> ObtenerCantidadDecimalesProductosAsync();
        Task<int> ObtenerCantidadDecimalesStockAsync();
        Task<string> ObtenerValorAsync(string unModulo, string unParametro);
        Task ActualizarValorAsync(string unModulo, string unParametro, string nuevoValor);
    }

}
