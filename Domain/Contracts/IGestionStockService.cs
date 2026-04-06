using Domain.DTO;

namespace Domain.Contracts;

public interface IGestionStockService
{
    Task<List<GestionStockItemDto>> BuscarAsync(
        IEnumerable<int>? proveedorIds,
        IEnumerable<int>? rubroIds,
        string?           texto);

    Task<AjusteStockResultDto> AjustarStockAsync(int productoId, decimal nuevoStock);
}
