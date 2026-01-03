using Domain.DTO;


namespace Domain.Contracts
{
    public interface IVentasEstadisticasService
    {
        Task<List<VentaResumenDto>> GetResumenVentasAsync(DateTime desde, DateTime hasta, int? clienteId, int? vendedorId, int? proveedorId);
        Task<List<VentaDetalleDto>> GetDetalleVentaAsync(long ventaId);

    }

}
