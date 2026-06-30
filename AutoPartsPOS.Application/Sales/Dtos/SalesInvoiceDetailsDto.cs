using AutoPartsPOS.Domain.Common;

namespace AutoPartsPOS.Application.Sales.Dtos;

public sealed record SalesInvoiceDetailsDto(
    long Id,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    string Status,
    InvoicePaymentStatus PaymentStatus,
    bool IsVoided,
    decimal SubtotalAmount,
    decimal DiscountAmount,
    decimal NetTotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    string? Notes,
    IReadOnlyList<SalesInvoiceItemDto> Items);
