using Domain.DTO;

namespace Domain.Contracts;

public interface IDashboardClienteService
{
    /// <summary>
    /// KPIs globales de ventas a clientes.
    /// clienteIds = null → todos los clientes.
    /// </summary>
    Task<ClienteMetricasDto> GetMetricasAsync(
        DateTime     desde,
        DateTime     hasta,
        List<int>?   clienteIds);

    /// <summary>
    /// Datos para los gráficos del modal de Clientes.
    /// clienteIds = null → todos los clientes.
    /// </summary>
    Task<ClienteGraficosDto> GetGraficosAsync(
        DateTime     desde,
        DateTime     hasta,
        List<int>?   clienteIds);
}
