namespace AutoPartsPOS.Application.Suppliers.Dtos;

public sealed record SupplierDto(
    long Id,
    string NameAr,
    string? Phone,
    string? Address,
    string? Notes,
    bool IsActive);
