namespace AutoPartsPOS.Application.Insights.Dtos;

public sealed record ReorderSuggestionDto(
    long ProductId,
    string ProductCode,
    string ProductNameAr,
    decimal ExpectedMonthlySales,
    decimal CurrentStock,
    decimal SuggestedQuantity);
