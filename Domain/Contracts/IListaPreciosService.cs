using Domain.DTO;

namespace Domain.Contracts;

public interface IListaPreciosService
{
    Task<List<ListaPrecioItemDto>> BuscarAsync(
        IEnumerable<int>? proveedorIds,
        IEnumerable<int>? rubroIds,
        string?           texto);
}
