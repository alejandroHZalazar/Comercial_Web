using Domain.DTO;

namespace Domain.Contracts;

public interface IListaPreciosService
{
    Task<List<ListaPrecioItemDto>> BuscarAsync(
        IEnumerable<int>? proveedorIds,
        IEnumerable<int>? rubroIds,
        string?           texto);

    /// <summary>Productos activos con stock estrictamente mayor que cero, con costo, precio de lista y totales.</summary>
    Task<List<StockExistenciaDto>> StockExistenciaAsync();
}
