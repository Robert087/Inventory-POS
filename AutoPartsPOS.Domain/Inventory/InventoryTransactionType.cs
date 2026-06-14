namespace AutoPartsPOS.Domain.Inventory;

public enum InventoryTransactionType
{
    Purchase = 1,
    Sale = 2,
    Adjustment = 3,
    VoidPurchase = 4,
    VoidSale = 5
}
