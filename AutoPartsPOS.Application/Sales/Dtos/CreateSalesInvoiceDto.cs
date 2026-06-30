using AutoPartsPOS.Domain.Common;

namespace AutoPartsPOS.Application.Sales.Dtos;

public sealed class CreateSalesInvoiceDto
{
    public string InvoiceNumber { get; init; } = string.Empty;

    public DateOnly InvoiceDate { get; init; }

    public string? Notes { get; init; }

    public decimal DiscountAmount { get; init; }

    public InvoicePaymentStatus? PaymentStatus { get; init; }

    public decimal PaidAmount { get; init; }

    public IReadOnlyList<CreateSalesInvoiceItemDto> Items { get; init; } = [];
}
