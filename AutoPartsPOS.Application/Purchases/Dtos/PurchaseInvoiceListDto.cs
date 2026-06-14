namespace AutoPartsPOS.Application.Purchases.Dtos;

public sealed record PurchaseInvoiceListDto(
    long Id,
    string InvoiceNumber,
    long SupplierId,
    string SupplierNameAr,
    DateOnly InvoiceDate,
    string Status,
    decimal TotalAmount,
    string? Notes);
