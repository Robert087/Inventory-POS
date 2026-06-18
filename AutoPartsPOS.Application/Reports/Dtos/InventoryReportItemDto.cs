namespace AutoPartsPOS.Application.Reports.Dtos;

public sealed record InventoryReportItemDto(
    long ProductId,
    string ProductCode,
    string ProductNameAr,
    decimal CurrentStock,
    decimal CurrentAverageCost,
    decimal InventoryValue,
    decimal MinimumStock,
    bool IsLowStock);
