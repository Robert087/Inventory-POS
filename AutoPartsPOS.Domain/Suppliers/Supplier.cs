using AutoPartsPOS.Domain.Common;

namespace AutoPartsPOS.Domain.Suppliers;

public sealed class Supplier : AuditableEntity
{
    public string NameAr { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
}
