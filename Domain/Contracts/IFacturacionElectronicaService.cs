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
    /// Emite nota de crédito electrónica manual (sin devolución asociada).
    /// Corresponde a la rama "else" del escritorio (unaDevolucion == 0).
    /// </summary>
    Task<(bool ok, string? error, string? pdfUrl)> EmitirNotaCreditoManualAsync(NotaCreditoRequestDto dto, int puntoVenta);
}
