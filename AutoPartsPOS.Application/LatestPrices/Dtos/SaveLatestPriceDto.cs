namespace AutoPartsPOS.Application.LatestPrices.Dtos;

public sealed class SaveLatestPriceDto
{
    public long? Id { get; init; }

    public string ProductCode { get; init; } = string.Empty;

    public string NameAr { get; init; } = string.Empty;

    public decimal LatestPurchasePrice { get; init; }
}
