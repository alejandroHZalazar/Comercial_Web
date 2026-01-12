using Domain.DTO;
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
        Task<ProductoLineaOCDto?> TraerPorIdOCAsync(int id);
        Task<ProductoLineaOCDto?> BuscarPorCodProveedorOCAsync(string codProveedor, int proveedorId);
        Task<ProductoLineaOCDto?> BuscarPorCodBarrasOCAsync(string codBarras, int proveedorId);
        Task<List<Producto>> BuscarPorDescripcionAsync(string descripcion);
        Task<int> CrearAsync(Producto producto);
        Task ActualizarAsync(Producto producto);
        Task EliminarAsync(int id); // baja lógica
        Task<int> ObtenerDecimalesAsync();         // para precios
        Task<int> ObtenerDecimalesStockAsync();    // para cantidades
        Task<List<ProductoDto>> TraerProductosProveedorAsync(int proveedorId);       
        Task<ProductoLineaOCDto?> TraerProductoParaEditarAsync(int productoId);
        Task<ProductoLineaOCDto?> BuscarPorDescripcionExactaAsync(string descripcion, int proveedorId);
        Task<List<ProductoCantMinimaDto>> TraerCantMinPorProveedorAsync(int proveedorId);


    }

}
