namespace AutoPartsPOS.Application.Catalog.Dtos;

public sealed class SaveProductDto
{
    public long? Id { get; init; }

    public string ProductCode { get; init; } = string.Empty;

    public string? Barcode { get; init; }

    public string NameAr { get; init; } = string.Empty;

    public long CategoryId { get; init; }

    public decimal PurchasePrice { get; init; }

    public decimal SellingPrice { get; init; }

    public decimal CurrentStock { get; init; }

    public decimal MinimumStock { get; init; }

    public bool IsActive { get; init; } = true;
}
