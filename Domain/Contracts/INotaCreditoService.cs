using Domain.DTO;
namespace Domain.Contracts;
public interface INotaCreditoService
{
    Task<NotaCreditoDatosIniciales> GetDatosInicialesAsync();
    /// <summary>
    /// Registra la NC: si facturaElectronica=1 y facturaAsociada>0 emite comprobante fiscal.
    /// Siempre guarda en DB (sp_clientes_ADD_NC emulado con EF).
    /// Retorna (cobroId, errorMsg). errorMsg es null si OK.
    /// </summary>
    Task<(int cobroId, string? error)> ProcesarNCAsync(NotaCreditoRequestDto dto);
    Task<int> ProcesarNDAsync(int clienteId, decimal importe, string observaciones);
}
