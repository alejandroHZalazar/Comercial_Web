using Domain.DTO;

namespace Domain.Contracts;

public interface ICuentaCorrienteService
{
    Task<List<CuentaCorrienteDto>> GetMovimientosAsync(int clienteId);
    Task<EstadoCCDto> GetEstadoCCAsync(int clienteId);
    Task<List<SaldoClienteDto>> GetSaldosDeudoresAsync(
        List<int> provincias,
        List<int> localidades,
        List<int> vendedores,
        List<int> zonas);
}
