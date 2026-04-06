using Domain.DTO;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Contracts
{
    public interface IOrdenCompraService
    {
        Task<List<OrdenCompra>> GetOrdenesAsync();
        Task<OrdenCompra?> GetOrdenByIdAsync(long id);
        Task<List<OrdenCompraDetalle>> GetDetallesByOrdenIdAsync(long ordenId);

        Task<long> CrearOrdenAsync(OrdenCompra orden, List<OrdenCompraDetalle> detalles);
        Task ActualizarOrdenAsync(OrdenCompra orden, List<OrdenCompraDetalle> detalles);
        Task EliminarOrdenAsync(long id); // baja lógica o eliminación física
        Task<List<ProductoAPedirOCDto>> TraerListaProdAPedirAsync(int proveedorId, DateTime desde, DateTime hasta);
        Task<List<ProductoAPedirImprimirOCDto>> TraerListaProdAPedirImprimirAsync(int proveedorId, DateTime desde, DateTime hasta);
        Task<long> GrabarOrdenCompraAsync(int proveedorId, decimal total, decimal iva, decimal recargo, decimal descuento, List<OrdenCompraDetalle> detalles, IProgress<int> progreso, CancellationToken ct);
        Task<List<OrdenCompraPrint>> OrdenCompraImprimirAsync(int id);
        Task<ProductoLineaOCDto?> BuscarPorCodProveedorOCAsync(string codProveedor, int proveedorId);
        Task<ProductoLineaOCDto?> BuscarPorCodBarrasOCAsync(string codBarras, int proveedorId);
        Task<ProductoLineaOCDto?> TraerPorIdOCAsync(int id);
        Task<ProductoLineaOCDto?> BuscarPorDescripcionExactaAsync(string descripcion, int proveedorId);
        Task<List<ProductoCantMinimaDto>> TraerCantMinPorProveedorAsync(int proveedorId);
    }
}
