using Domain.DTO;

namespace Domain.Contracts;

public interface IFacturacionElectronicaService
{
    /// <summary>
    /// Emite factura electrónica para una venta existente (TusFacturas.app).
    /// Incluye reintentos por error de "sumatorias finales".
    /// </summary>
    Task<FacturaElectronicaResultDto> EmitirFacturaVentaAsync(long ventaId);

    /// <summary>
    /// Emite nota de crédito electrónica manual (sin devolución asociada), o real si
    /// dto.IdDevolucion > 0. Si el detalle supera 130 ítems, se emiten varias NC y el
    /// resultado incluye cada una en Comprobantes.
    /// </summary>
    Task<NotaCreditoElectronicaResultDto> EmitirNotaCreditoManualAsync(NotaCreditoRequestDto dto, int puntoVenta);
}
