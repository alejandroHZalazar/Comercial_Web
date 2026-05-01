using Domain.DTO;

namespace Domain.Contracts;

public interface IDevolucionService
{
    Task<long>                    GrabarDevolucionAsync(DevolucionRequestDto dto);
    Task<DevolucionImpresionDto?> GetDevolucionParaImpresionAsync(long devolucionId);
}
