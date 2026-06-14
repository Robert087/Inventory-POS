namespace AutoPartsPOS.Application.Sales.Dtos;

public sealed class CreateSalesInvoiceDto
{
    public string InvoiceNumber { get; init; } = string.Empty;

    public DateOnly InvoiceDate { get; init; }

    public string? Notes { get; init; }

    public decimal DiscountAmount { get; init; }

    public IReadOnlyList<CreateSalesInvoiceItemDto> Items { get; init; } = [];
}
