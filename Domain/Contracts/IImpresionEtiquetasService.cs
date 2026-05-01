using Domain.DTO;

namespace Domain.Contracts;

public interface IImpresionEtiquetasService
{
    Task<List<ImpresionEtiquetasItemDto>> BuscarAsync(
        IEnumerable<int>? proveedorIds,
        IEnumerable<int>? rubroIds,
        string?           texto);
}
