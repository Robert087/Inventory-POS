using AutoPartsPOS.Application.Analytics.Dtos;
using AutoPartsPOS.Application.Analytics.Interfaces;

namespace AutoPartsPOS.Application.Analytics.Services;

public sealed class SalesAnalyticsService(ISalesAnalyticsRepository salesAnalyticsRepository) : ISalesAnalyticsService
{
    public Task<IReadOnlyList<TopSellingProductDto>> GetTopSellingProductsAsync(DateOnly fromDate, DateOnly toDate, int take = 10, CancellationToken cancellationToken = default)
    {
        return salesAnalyticsRepository.GetTopSellingProductsAsync(fromDate, toDate, take, cancellationToken);
    }

    public Task<IReadOnlyList<SlowMovingProductDto>> GetSlowMovingProductsAsync(int days, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-Math.Max(days, 1)));
        return salesAnalyticsRepository.GetSlowMovingProductsAsync(cutoffDate, cancellationToken);
    }

    public Task<SalesSummaryDto> GetDailySalesSummaryAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        return salesAnalyticsRepository.GetSalesSummaryAsync(date, date, cancellationToken);
    }

    public Task<SalesSummaryDto> GetMonthlySalesSummaryAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var fromDate = new DateOnly(year, month, 1);
        var toDate = fromDate.AddMonths(1).AddDays(-1);
        return salesAnalyticsRepository.GetSalesSummaryAsync(fromDate, toDate, cancellationToken);
    }
}
