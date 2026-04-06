using Domain.DTO;

namespace Domain.Contracts;

public interface IMovimientosProductosService
{
    // ---- Tab 1: Lotes ----
    Task<List<LoteIngresoDto>> BuscarLotesAsync(
        IEnumerable<int>? proveedorIds,
        string?           texto,
        string?           nroComprobante,
        DateTime?         fechaDesde,
        DateTime?         fechaHasta);

    Task<List<LoteIngresoDetalleDto>> BuscarDetallesLoteAsync(
        string    nroComprobante,
        DateTime  fechaMov);

    // ---- Tab 2: Movimientos por producto ----
    Task<List<ProductoConMovimientosDto>> BuscarProductosAsync(
        string?   texto,
        DateTime? fechaDesde,
        DateTime? fechaHasta);

    Task<List<MovimientoItemDto>> BuscarMovimientosProductoAsync(
        int       productoId,
        DateTime? fechaDesde,
        DateTime? fechaHasta);
}
