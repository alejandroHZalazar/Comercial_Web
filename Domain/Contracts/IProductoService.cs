using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Contracts
{
    public interface IProductoService
    {
        Task<List<Producto>> TraerTodosAsync();
        Task<Producto?> TraerPorIdAsync(int id);
        Task<List<Producto>> BuscarPorCodProveedorAsync(string codProveedor);
        Task<List<Producto>> BuscarPorDescripcionAsync(string descripcion);
        Task<int> CrearAsync(Producto producto);
        Task ActualizarAsync(Producto producto);
        Task EliminarAsync(int id); // baja lógica
        Task<int> ObtenerDecimalesAsync();         // para precios
        Task<int> ObtenerDecimalesStockAsync();    // para cantidades
    }

}
