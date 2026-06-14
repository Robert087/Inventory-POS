namespace AutoPartsPOS.Application.Analytics.Dtos;

public sealed record SlowMovingProductDto(long ProductId, string ProductNameAr, decimal CurrentStock, DateOnly? LastSaleDate);
