namespace AutoPartsPOS.Application.Sales.Dtos;

public sealed record SalesInvoiceDetailsDto(
    long Id,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    string Status,
    decimal SubtotalAmount,
    decimal DiscountAmount,
    decimal NetTotalAmount,
    string? Notes,
    IReadOnlyList<SalesInvoiceItemDto> Items);
