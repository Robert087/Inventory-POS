using AutoPartsPOS.Application.Analytics.Dtos;

namespace AutoPartsPOS.Application.Analytics.Interfaces;

public interface ISalesAnalyticsService
{
    Task<IReadOnlyList<TopSellingProductDto>> GetTopSellingProductsAsync(DateOnly fromDate, DateOnly toDate, int take = 10, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SlowMovingProductDto>> GetSlowMovingProductsAsync(int days, CancellationToken cancellationToken = default);

    Task<SalesSummaryDto> GetDailySalesSummaryAsync(DateOnly date, CancellationToken cancellationToken = default);

    Task<SalesSummaryDto> GetMonthlySalesSummaryAsync(int year, int month, CancellationToken cancellationToken = default);
}
