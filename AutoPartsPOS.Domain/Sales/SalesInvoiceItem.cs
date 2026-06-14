using AutoPartsPOS.Domain.Catalog;
using AutoPartsPOS.Domain.Common;

namespace AutoPartsPOS.Domain.Sales;

public sealed class SalesInvoiceItem : Entity
{
    public long SalesInvoiceId { get; set; }

    public SalesInvoice? SalesInvoice { get; set; }

    public long ProductId { get; set; }

    public Product? Product { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }
}
