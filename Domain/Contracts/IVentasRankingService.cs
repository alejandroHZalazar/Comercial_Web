using Domain.DTO;

namespace Domain.Contracts
{
    public interface IVentasRankingService
    {
        Task<List<RankingItemDto>> TraerVentasRankingProductosAsync(DateTime desde, DateTime hasta);
        Task<List<RankingItemDto>> TraerVentasRankingClientesAsync(DateTime desde, DateTime hasta);
    }

}
