namespace AutoPartsPOS.Application.Catalog.Dtos;

public sealed class ReplenishProductStockDto
{
    public long ProductId { get; init; }

    public decimal Quantity { get; init; }

    public decimal PurchasePrice { get; init; }
}
