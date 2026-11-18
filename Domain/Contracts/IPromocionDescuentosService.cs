using Domain.DTO;

namespace Domain.Contracts;

/// <summary>
/// Promociones por cantidad (NxM) y por porcentaje. Un único motor, en el servidor, usado por
/// Ventas, Venta minorista y Pedidos. Independiente de IPromocionService (combos por slots).
/// </summary>
public interface IPromocionDescuentosService
{
    /// <summary>Motor activo: parámetro promociones/descuentos = '1' y tablas creadas. Con false no se toca ninguna tabla nueva.</summary>
    Task<bool> HabilitadoAsync();

    /// <summary>Las tablas existen (permite usar el ABM aunque el motor esté apagado).</summary>
    Task<bool> TablasListasAsync();

    // ── Evaluación (grilla) ─────────────────────────────────────────────────
    Task<PromoEvalRespuestaDto> EvaluarAsync(IEnumerable<PromoEvalLineaDto> lineas);

    // ── Guardado: valida lo que envió el navegador y expande NxM en cobradas / bonificadas ──
    /// <exception cref="InvalidOperationException">Las promociones enviadas no coinciden con las vigentes.</exception>
    Task<List<PromoGuardarSegmento>> ResolverParaGuardarAsync(IReadOnlyList<PromoGuardarLinea> lineas);

    /// <summary>Promoción registrada por línea de pedido (para reunir cobradas+bonificadas al recargar un pedido).</summary>
    Task<Dictionary<long, int>> ReglaPorLineaPedidoAsync(IEnumerable<long> lineasPedido);

    // ── ABM ─────────────────────────────────────────────────────────────────
    Task<List<PromoReglaListDto>> ListarAsync();
    Task<PromoReglaDetalleDto?>   ObtenerAsync(int id);
    Task<int>                     GuardarAsync(PromoReglaDetalleDto dto, string? usuario);
    Task                          EliminarAsync(int id);
    Task<bool>                    ToggleActivaAsync(int id);
    Task<PromoBusquedaProductosDto> BuscarProductosAsync(IEnumerable<int>? proveedores, IEnumerable<int>? rubros, string? texto, int max);
}
