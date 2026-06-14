namespace AutoPartsPOS.Application.Catalog.Dtos;

public sealed class SaveCategoryDto
{
    public long? Id { get; init; }

    public string NameAr { get; init; } = string.Empty;

    public string? Description { get; init; }

    public bool IsActive { get; init; } = true;
}
