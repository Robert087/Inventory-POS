namespace AutoPartsPOS.Application.Sales.Dtos;

public sealed record SalesInvoiceItemDto(
    long Id,
    long ProductId,
    string ProductNameAr,
    decimal Quantity,
    decimal UnitPrice,
    decimal TotalPrice);
