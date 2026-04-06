using Domain.DTO;

namespace Domain.Contracts;

public interface IDashboardProveedorService
{
    /// <summary>
    /// Indicadores KPI de compras a proveedores.
    /// proveedorIds = null o vacío → todos los proveedores.
    /// </summary>
    Task<ProveedorMetricasDto> GetMetricasAsync(
        DateTime    desde,
        DateTime    hasta,
        List<int>?  proveedorIds);

    /// <summary>
    /// Datos para los tres gráficos del modal.
    /// proveedorIds = null o vacío → todos los proveedores.
    /// </summary>
    Task<ProveedorGraficosDto> GetGraficosAsync(
        DateTime    desde,
        DateTime    hasta,
        List<int>?  proveedorIds);
}
