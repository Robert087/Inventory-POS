using AutoPartsPOS.Domain.Common;

namespace AutoPartsPOS.Domain.Catalog;

public sealed class ProductCategory : AuditableEntity
{
    public string NameAr { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = [];
}
