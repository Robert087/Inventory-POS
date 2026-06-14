namespace AutoPartsPOS.Application.Purchases.Dtos;

public sealed record PurchaseInvoiceDetailsDto(
    long Id,
    string InvoiceNumber,
    long SupplierId,
    string SupplierNameAr,
    DateOnly InvoiceDate,
    string Status,
    decimal TotalAmount,
    string? Notes,
    IReadOnlyList<PurchaseInvoiceItemDto> Items);
