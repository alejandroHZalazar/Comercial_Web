using Domain.DTO;

namespace Domain.Contracts;

public interface ICambiosPreciosService
{
    Task<List<CambiosPreciosItemDto>> BuscarAsync(
        IEnumerable<int>? proveedorIds,
        IEnumerable<int>? rubroIds,
        string?           texto);

    /// <summary>
    /// Actualiza los tres precios directamente (P.Prov, P.S/IVA, Costo).
    /// Guarda log de precios anteriores y establece Baja=false.
    /// No recalcula desde ganancia/descuento del proveedor.
    /// </summary>
    Task<CambioPrecioResultDto> ActualizarPrecioDirectoAsync(ActualizarPrecioDirectoRequest req);
}
