using AutoPartsPOS.Domain.Common;
using AutoPartsPOS.Domain.Suppliers;

namespace AutoPartsPOS.Domain.Purchases;

public sealed class PurchaseInvoice : AuditableEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public long SupplierId { get; set; }

    public Supplier? Supplier { get; set; }

    public DateOnly InvoiceDate { get; set; }

    public string? Notes { get; set; }

    public PurchaseInvoiceStatus Status { get; set; } = PurchaseInvoiceStatus.Posted;

    public decimal TotalAmount { get; set; }

    public DateTimeOffset? VoidedAt { get; set; }

    public string? VoidReason { get; set; }

    public ICollection<PurchaseInvoiceItem> Items { get; set; } = [];
}
