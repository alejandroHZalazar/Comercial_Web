using Domain.DTO;

namespace Domain.Contracts;

public interface IFacturacionLotesService
{
    /// <summary>
    /// Equivalente a sp_Ventas_TraerSinFacturarPorFecha.
    /// Devuelve ventas del período que NO tienen comprobante fiscal de tipo 'Factura'.
    /// </summary>
    Task<List<VentaNoFacturadaDto>> GetVentasNoFacturadasAsync(DateTime desde, DateTime hasta);
}
