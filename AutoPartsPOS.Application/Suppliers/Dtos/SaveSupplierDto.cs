namespace AutoPartsPOS.Application.Suppliers.Dtos;

public sealed class SaveSupplierDto
{
    public long? Id { get; init; }

    public string NameAr { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public string? Address { get; init; }

    public string? Notes { get; init; }

    public bool IsActive { get; init; } = true;
}
