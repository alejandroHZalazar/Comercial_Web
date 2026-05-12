using Domain.DTO;

namespace Domain.Contracts;

public interface IDevolucionService
{
    Task<long>                            GrabarDevolucionAsync(DevolucionRequestDto dto);
    Task<DevolucionImpresionDto?>         GetDevolucionParaImpresionAsync(long devolucionId);
    Task<List<DevolucionReporteItemDto>>  BuscarDevolucionesAsync(DateTime desde, DateTime hasta, List<int> clienteIds);
}
