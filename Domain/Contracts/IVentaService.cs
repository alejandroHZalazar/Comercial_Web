using Domain.DTO;
using Domain.Entities;

namespace Domain.Contracts;

public interface IVentaService
{
    Task<List<Impuesto>>       GetImpuestosAsync();
    Task<VentaParametrosDto>   GetParametrosAsync(string machineName);
    Task<ClienteVentaDataDto?> GetClienteDataAsync(int clienteId);
    Task<List<Proveedore>>     GetProveedoresActivosAsync();
    Task<long>                 GrabarVentaAsync(GrabarVentaRequestDto dto);
    Task<(bool abierta, int cajaId)> GetCajaAbiertaAsync(int usuarioId);
    Task<VentaImpresionDto?>   GetVentaParaImpresionAsync(long ventaId);
    Task<FacturaElectronicaResultDto> EmitirFacturaElectronicaAsync(long ventaId);
    /// <summary>Devuelve precios actuales de productos por lista de IDs.</summary>
    Task<List<ProductoPrecioActualDto>> GetPreciosActualesAsync(List<int> productoIds, bool dolariza, decimal cotizDolar);
    /// <summary>Devuelve stock actual de productos por lista de IDs. Dict: productoId → stock.</summary>
    Task<Dictionary<int, decimal>> GetStockProductosAsync(List<int> productoIds);
}
