namespace AutoPartsPOS.Application.Reports.Dtos;

public sealed record InventoryReportDto(
    decimal TotalStockQuantity,
    decimal InventoryValue,
    int LowStockCount,
    IReadOnlyList<InventoryReportItemDto> Items);
