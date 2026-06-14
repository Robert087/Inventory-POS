using AutoPartsPOS.Domain.Common;

namespace AutoPartsPOS.Domain.Catalog;

public sealed class Product : AuditableEntity
{
    public string ProductCode { get; set; } = string.Empty;

    public string? Barcode { get; set; }

    public string NameAr { get; set; } = string.Empty;

    public long CategoryId { get; set; }

    public ProductCategory? Category { get; set; }

    public decimal PurchasePrice { get; set; }

    public decimal SellingPrice { get; set; }

    public decimal CurrentStock { get; set; }

    public decimal MinimumStock { get; set; }

    public bool IsActive { get; set; } = true;
}
