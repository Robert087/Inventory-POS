namespace AutoPartsPOS.Application.Inventory.Dtos;

public sealed record InventoryTransactionDto(
    long Id,
    long ProductId,
    string ProductNameAr,
    string TransactionType,
    decimal Quantity,
    decimal BalanceAfter,
    string ReferenceType,
    long ReferenceId,
    string ReferenceNumber,
    DateTimeOffset TransactionDate,
    string? Notes)
{
    public decimal DisplayQuantity => Math.Abs(Quantity);
}
