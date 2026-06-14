namespace AutoPartsPOS.Application.Catalog.Dtos;

public sealed record CategoryDto(
    long Id,
    string NameAr,
    string? Description,
    bool IsActive);
