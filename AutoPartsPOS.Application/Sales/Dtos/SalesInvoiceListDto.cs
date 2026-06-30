namespace AutoPartsPOS.Application.Sales.Dtos;

public sealed record SalesInvoiceListDto(
    long Id,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    string Status,
    decimal SubtotalAmount,
    decimal DiscountAmount,
    decimal NetTotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    string? Notes);
