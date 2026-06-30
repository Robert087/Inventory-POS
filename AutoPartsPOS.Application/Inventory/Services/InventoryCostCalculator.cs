namespace AutoPartsPOS.Application.Inventory.Services;

public static class InventoryCostCalculator
{
    public static decimal CalculateWeightedAverageCost(
        decimal currentStock,
        decimal currentAverageCost,
        decimal purchaseQuantity,
        decimal purchaseUnitCost)
    {
        var newStock = currentStock + purchaseQuantity;

        if (newStock <= 0)
        {
            return 0;
        }

        var currentValue = currentStock * currentAverageCost;
        var purchaseValue = purchaseQuantity * purchaseUnitCost;

        return decimal.Round((currentValue + purchaseValue) / newStock, 4);
    }

    public static decimal CalculateAverageCostAfterPurchaseReversal(
        decimal currentStock,
        decimal currentAverageCost,
        decimal purchaseQuantity,
        decimal purchaseUnitCost)
    {
        var newStock = currentStock - purchaseQuantity;

        if (newStock <= 0)
        {
            return 0;
        }

        var currentValue = currentStock * currentAverageCost;
        var reversedValue = purchaseQuantity * purchaseUnitCost;
        var remainingValue = currentValue - reversedValue;

        if (remainingValue < 0)
        {
            throw new InvalidOperationException("Voiding purchase invoice would create negative inventory value.");
        }

        return decimal.Round(remainingValue / newStock, 4);
    }
}
