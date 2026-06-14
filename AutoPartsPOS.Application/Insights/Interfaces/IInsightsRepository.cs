using AutoPartsPOS.Application.Insights.Dtos;

namespace AutoPartsPOS.Application.Insights.Interfaces;

public interface IInsightsRepository
{
    Task<IReadOnlyList<LowStockInsightDto>> GetLowStockProductsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReorderSuggestionDto>> GetReorderSuggestionsAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
}
