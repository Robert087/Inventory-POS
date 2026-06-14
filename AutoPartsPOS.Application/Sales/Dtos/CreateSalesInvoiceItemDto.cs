namespace AutoPartsPOS.Application.Sales.Dtos;

public sealed class CreateSalesInvoiceItemDto
{
    public long ProductId { get; init; }

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }
}
