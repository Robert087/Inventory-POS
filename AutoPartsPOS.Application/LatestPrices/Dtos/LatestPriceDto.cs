namespace AutoPartsPOS.Application.LatestPrices.Dtos;

public sealed record LatestPriceDto(
    long Id,
    string ProductCode,
    string NameAr,
    decimal LatestPurchasePrice);
