namespace AutoPartsPOS.Application.Purchases.Dtos;

public sealed class CreatePurchaseInvoiceDto
{
    public string InvoiceNumber { get; init; } = string.Empty;

    public long SupplierId { get; init; }

    public DateOnly InvoiceDate { get; init; }

    public string? Notes { get; init; }

    public IReadOnlyList<CreatePurchaseInvoiceItemDto> Items { get; init; } = [];
}
