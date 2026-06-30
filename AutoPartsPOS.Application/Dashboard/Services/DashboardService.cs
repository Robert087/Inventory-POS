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
        var daySales = await GetDailySalesAsync(today, cancellationToken);
        var monthStatistics = await GetMonthlyStatisticsAsync(today.Year, today.Month, cancellationToken);
        var inventory = await reportingService.GetInventoryReportAsync(cancellationToken);
        var topSelling = await smartInsightsService.GetTopSellingProductsAsync(5, cancellationToken);
        var lowStock = await smartInsightsService.GetLowStockProductsAsync(cancellationToken);
        var slowMoving = await smartInsightsService.GetSlowMovingProductsAsync(cancellationToken);
        var reorder = await smartInsightsService.GetReorderSuggestionsAsync(cancellationToken);

        return new DashboardDto(
            daySales,
            monthStatistics.Sales,
            monthStatistics.InvoiceCount,
            inventory.InventoryValue,
            monthStatistics.NetProfit,
            lowStock.Count,
            topSelling,
            lowStock.Take(10).ToList(),
            slowMoving.Take(10).ToList(),
            reorder.Take(10).ToList());
    }

    public async Task<decimal> GetDailySalesAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var summary = await salesAnalyticsService.GetDailySalesSummaryAsync(date, cancellationToken);
        return summary.NetSales;
    }

    public async Task<MonthlyDashboardStatisticsDto> GetMonthlyStatisticsAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var sales = await salesAnalyticsService.GetMonthlySalesSummaryAsync(year, month, cancellationToken);
        var profit = await reportingService.GetProfitReportAsync(monthStart, monthEnd, cancellationToken);

        return new MonthlyDashboardStatisticsDto(sales.NetSales, sales.InvoiceCount, profit.Profit);
    }
}
