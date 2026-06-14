using AutoPartsPOS.Domain.Common;

namespace AutoPartsPOS.Domain.Sales;

public sealed class SalesInvoice : AuditableEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public DateOnly InvoiceDate { get; set; }

    public string? Notes { get; set; }

    public SalesInvoiceStatus Status { get; set; } = SalesInvoiceStatus.Posted;

    public decimal SubtotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal NetTotalAmount { get; set; }

    public DateTimeOffset? VoidedAt { get; set; }

    public string? VoidReason { get; set; }

    public ICollection<SalesInvoiceItem> Items { get; set; } = [];
}
