using Domain.DTO;
using static Domain.DTO.IngresoProductosDTO;

namespace Domain.Contracts
{
    public interface IIngresoProductosService
    {
        Task<List<ProductosPorProveedorDTO>> TraerProductosProveedorAsync(int proveedorId, int decCant, int decStock);
        Task<List<OrdenCompraBuscarDTO>> TraerOrdenesCompraSinProcesarPorProveedor(int unProveedorint, int decCant);
        Task<List<OrdenCompraBuscarDetalleDTO>> TraerOrdenCompraDetalle(int unaOrden, int decCant, int decStock);
        Task<List<OrdenCompraBuscarDTO>> TraerOrdenesCompraSinProcesarPorProveedorFecha(int unProveedorint, DateTime desde, DateTime hasta, int decCant);
        Task ProcesarIngresoAsync(List<IngresoProductoDetalleDto> items, IProgress<int> progress);
    }
}
