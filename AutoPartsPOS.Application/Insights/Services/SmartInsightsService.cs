using AutoPartsPOS.Application.Analytics.Dtos;
using AutoPartsPOS.Application.Analytics.Interfaces;
using AutoPartsPOS.Application.Insights.Dtos;
using AutoPartsPOS.Application.Insights.Interfaces;
using AutoPartsPOS.Application.Settings.Interfaces;

namespace AutoPartsPOS.Application.Insights.Services;

public sealed class SmartInsightsService(
    IInsightsRepository insightsRepository,
    ISalesAnalyticsService salesAnalyticsService,
    IApplicationSettingsService settingsService) : ISmartInsightsService
{
    public Task<IReadOnlyList<LowStockInsightDto>> GetLowStockProductsAsync(CancellationToken cancellationToken = default) =>
        insightsRepository.GetLowStockProductsAsync(cancellationToken);

    public async Task<IReadOnlyList<TopSellingProductDto>> GetTopSellingProductsAsync(int take = 10, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.LoadAsync(cancellationToken);
        var toDate = DateOnly.FromDateTime(DateTime.Today);
        var fromDate = toDate.AddDays(-Math.Max(settings.TopSellingDays, 1) + 1);
        return await salesAnalyticsService.GetTopSellingProductsAsync(fromDate, toDate, take, cancellationToken);
    }

    public async Task<IReadOnlyList<SlowMovingProductDto>> GetSlowMovingProductsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.LoadAsync(cancellationToken);
        return await salesAnalyticsService.GetSlowMovingProductsAsync(settings.SlowMovingDays, cancellationToken);
    }

    public async Task<IReadOnlyList<ReorderSuggestionDto>> GetReorderSuggestionsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.LoadAsync(cancellationToken);
        var toDate = DateOnly.FromDateTime(DateTime.Today);
        var fromDate = toDate.AddDays(-Math.Max(settings.TopSellingDays, 1) + 1);
        return await insightsRepository.GetReorderSuggestionsAsync(fromDate, toDate, cancellationToken);
    }
}
