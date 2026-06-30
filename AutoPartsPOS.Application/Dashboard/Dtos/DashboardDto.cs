using AutoPartsPOS.Application.Analytics.Dtos;
using AutoPartsPOS.Application.Insights.Dtos;

namespace AutoPartsPOS.Application.Dashboard.Dtos;

public sealed record DashboardDto(
    decimal TodaySales,
    decimal CurrentMonthSales,
    int InvoiceCount,
    decimal InventoryValue,
    decimal NetProfit,
    int LowStockCount,
    IReadOnlyList<TopSellingProductDto> TopSellingProducts,
    IReadOnlyList<LowStockInsightDto> LowStockProducts,
    IReadOnlyList<SlowMovingProductDto> SlowMovingProducts,
    IReadOnlyList<ReorderSuggestionDto> ReorderSuggestions);
