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
        Task<List<Producto>> BuscarPorDescripcionAsync(string descripcion);
        Task CrearAsync(ProductoDetallesDTO vm);
        Task ActualizarAsync(ProductoDetallesDTO producto);
        Task EliminarAsync(int id); // baja lógica       
        Task<List<ProductoDto>> TraerProductosProveedorAsync(int proveedorId); 
        Task<List<Producto>> GetByCodProveedorProveedorAsync(string codProveedor, int proveedorId);
        Task<List<Producto>> GetByCodBarrasProveedorAsync(string codBarra, int proveedorId);
        Task<List<Producto>> GetByCodProveedorAsync(string codProveedor);
        Task<List<Producto>> GetByCodBarrasAsync(string codBarra);
        Task<ProductoDetallesDTO> traerDetalleAsync(int id, int decCant, int decStock);

    }

}
