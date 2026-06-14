namespace AutoPartsPOS.Application.Insights.Dtos;

public sealed record LowStockInsightDto(
    long ProductId,
    string ProductCode,
    string ProductNameAr,
    decimal CurrentStock,
    decimal MinimumStock);
