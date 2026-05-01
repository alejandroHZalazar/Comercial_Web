using Domain.DTO;

namespace Domain.Contracts;

public interface IReporteVentasService
{
    Task<List<ReporteVentaItem>> BuscarVentasAsync(
        DateTime? desde, DateTime? hasta, List<int> clientes);
}
