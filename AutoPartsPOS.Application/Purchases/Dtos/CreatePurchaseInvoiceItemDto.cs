namespace AutoPartsPOS.Application.Purchases.Dtos;

public sealed class CreatePurchaseInvoiceItemDto
{
    public long ProductId { get; init; }

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }
}
