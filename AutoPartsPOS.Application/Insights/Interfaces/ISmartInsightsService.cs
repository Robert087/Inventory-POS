using AutoPartsPOS.Application.Analytics.Dtos;
using AutoPartsPOS.Application.Insights.Dtos;

namespace AutoPartsPOS.Application.Insights.Interfaces;

public interface ISmartInsightsService
{
    Task<IReadOnlyList<LowStockInsightDto>> GetLowStockProductsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopSellingProductDto>> GetTopSellingProductsAsync(int take = 10, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SlowMovingProductDto>> GetSlowMovingProductsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReorderSuggestionDto>> GetReorderSuggestionsAsync(CancellationToken cancellationToken = default);
}
