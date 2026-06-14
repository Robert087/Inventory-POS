using AutoPartsPOS.Domain.Catalog;
using AutoPartsPOS.Domain.Common;

namespace AutoPartsPOS.Domain.Inventory;

public sealed class InventoryTransaction : Entity
{
    public long ProductId { get; set; }

    public Product? Product { get; set; }

    public InventoryTransactionType TransactionType { get; set; }

    public decimal Quantity { get; set; }

    public decimal BalanceAfter { get; set; }

    public InventoryReferenceType ReferenceType { get; set; }

    public long ReferenceId { get; set; }

    public DateTimeOffset TransactionDate { get; set; }

    public string? Notes { get; set; }
}
