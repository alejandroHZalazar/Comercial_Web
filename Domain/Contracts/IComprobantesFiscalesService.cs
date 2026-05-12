using Domain.DTO;

namespace Domain.Contracts;

public interface IComprobantesFiscalesService
{
    Task<List<ComprobanteEmitidoDto>> BuscarAsync(BuscarComprobantesDto filtros);
    Task<ComprobanteDetalleDto?>      GetDetalleAsync(long comprobanteId);
    Task<EstadisticasFEDto>           GetEstadisticasAsync(DateTime desde, DateTime hasta);
}
