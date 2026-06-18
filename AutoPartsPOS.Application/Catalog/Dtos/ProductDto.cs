namespace AutoPartsPOS.Application.Catalog.Dtos;

public sealed record ProductDto(
    long Id,
    string ProductCode,
    string? Barcode,
    string NameAr,
    long CategoryId,
    string CategoryNameAr,
    decimal PurchasePrice,
    decimal CurrentAverageCost,
    decimal SellingPrice,
    decimal CurrentStock,
    decimal MinimumStock,
    bool IsActive);
