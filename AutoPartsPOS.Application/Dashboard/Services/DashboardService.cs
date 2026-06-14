using AutoPartsPOS.Application.Analytics.Interfaces;
using AutoPartsPOS.Application.Dashboard.Dtos;
using AutoPartsPOS.Application.Dashboard.Interfaces;
using AutoPartsPOS.Application.Insights.Interfaces;
using AutoPartsPOS.Application.Reports.Interfaces;

namespace AutoPartsPOS.Application.Dashboard.Services;

public sealed class DashboardService(
    ISalesAnalyticsService salesAnalyticsService,
    IReportingService reportingService,
    ISmartInsightsService smartInsightsService) : IDashboardService
{
    public async Task<DashboardDto> LoadAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var todaySales = await salesAnalyticsService.GetDailySalesSummaryAsync(today, cancellationToken);
        var monthSales = await salesAnalyticsService.GetMonthlySalesSummaryAsync(today.Year, today.Month, cancellationToken);
        var inventory = await reportingService.GetInventoryReportAsync(cancellationToken);
        var topSelling = await smartInsightsService.GetTopSellingProductsAsync(5, cancellationToken);
        var lowStock = await smartInsightsService.GetLowStockProductsAsync(cancellationToken);
        var slowMoving = await smartInsightsService.GetSlowMovingProductsAsync(cancellationToken);
        var reorder = await smartInsightsService.GetReorderSuggestionsAsync(cancellationToken);

        return new DashboardDto(
            todaySales.NetSales,
            monthSales.NetSales,
            monthSales.InvoiceCount,
            inventory.InventoryValue,
            lowStock.Count,
            topSelling,
            lowStock.Take(10).ToList(),
            slowMoving.Take(10).ToList(),
            reorder.Take(10).ToList());
    }
}
