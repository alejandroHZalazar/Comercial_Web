using Domain.DTO;

namespace Domain.Contracts;

public interface IVentasExportarService
{
    Task<List<VentaResumenExportarDto>> GetResumenAsync(DateTime desde, DateTime hasta);
    Task<List<VentaDetalleExportarDto>> GetDetalleAsync(DateTime desde, DateTime hasta);
}
