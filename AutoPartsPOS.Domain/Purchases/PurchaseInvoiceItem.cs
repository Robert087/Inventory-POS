using AutoPartsPOS.Domain.Catalog;
using AutoPartsPOS.Domain.Common;

namespace AutoPartsPOS.Domain.Purchases;

public sealed class PurchaseInvoiceItem : Entity
{
    public long PurchaseInvoiceId { get; set; }

    public PurchaseInvoice? PurchaseInvoice { get; set; }

    public long ProductId { get; set; }

    public Product? Product { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }
}
