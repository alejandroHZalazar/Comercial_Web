using Domain.DTO;

namespace Domain.Contracts;

public interface IStockExistenciaPdfService
{
    /// <summary>Genera el PDF del listado de stock en existencia y devuelve sus bytes.</summary>
    byte[] Generar(StockExistenciaReporteDto datos);
}
