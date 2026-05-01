using Domain.DTO;

namespace Domain.Contracts;

public interface IPromocionService
{
    // ── ABM ─────────────────────────────────────────────────────────────────
    Task<List<PromocionListDto>>   GetListaAsync();
    Task<PromocionDetalleDto?>     GetDetalleAsync(int id);
    Task<int>                      GuardarAsync(PromocionDetalleDto dto);
    Task                           EliminarAsync(int id);
    Task                           ToggleActivaAsync(int id);

    // ── Búsqueda por producto (para ventas) ──────────────────────────────────
    Task<PromocionParaVentaDto?>   GetParaVentaAsync(int fkProducto);

    // ── Descuento de stock de componentes ────────────────────────────────────
    Task                           GuardarComponentesAsync(
                                       long                            fkVentaDetalle,
                                       IEnumerable<PromoComponenteDto> componentes);

    Task                           GuardarComponentesPorVentaAsync(
                                       long                            ventaId,
                                       int                             fkProductoPromo,
                                       IEnumerable<PromoComponenteDto> componentes);

    // ── Autocompletar: productos disponibles (no promo, sin baja) ────────────
    Task<List<SlotProdDto>>        BuscarProductosAsync(string texto);

    // ── Cálculo de promociones formables desde el carrito ────────────────────
    Task<CalcularPromocionesResultDto> CalcularPromocionesAsync(
        IEnumerable<ItemCarritoDto> carrito);
}
