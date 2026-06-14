namespace AutoPartsPOS.Application.Purchases.Dtos;

public sealed record PurchaseInvoiceItemDto(
    long Id,
    long ProductId,
    string ProductNameAr,
    decimal Quantity,
    decimal UnitPrice,
    decimal TotalPrice);
