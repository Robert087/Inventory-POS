using AutoPartsPOS.Application.Analytics.Dtos;

namespace AutoPartsPOS.Application.Analytics.Interfaces;

public interface ISalesAnalyticsRepository
{
    Task<IReadOnlyList<TopSellingProductDto>> GetTopSellingProductsAsync(DateOnly fromDate, DateOnly toDate, int take, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SlowMovingProductDto>> GetSlowMovingProductsAsync(DateOnly cutoffDate, CancellationToken cancellationToken = default);

    Task<SalesSummaryDto> GetSalesSummaryAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
}
