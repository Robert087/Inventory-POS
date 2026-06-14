namespace AutoPartsPOS.Application.Analytics.Dtos;

public sealed record TopSellingProductDto(long ProductId, string ProductNameAr, decimal QuantitySold, decimal SalesAmount);
