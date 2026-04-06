using Domain.DTO;

namespace Domain.Contracts;

public interface IDashboardVentasService
{
    Task<DashboardIndicadoresDto> GetIndicadoresAsync(DateTime desde, DateTime hasta);
    Task<DashboardVentasDto>      GetVentasAsync(DateTime desde, DateTime hasta);
}
