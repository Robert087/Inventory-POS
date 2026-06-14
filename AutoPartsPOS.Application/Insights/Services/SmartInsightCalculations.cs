namespace AutoPartsPOS.Application.Insights.Services;

public static class SmartInsightCalculations
{
    public static decimal CalculateExpectedMonthlySales(decimal quantitySold, int periodDays) =>
        decimal.Round(quantitySold / Math.Max(periodDays, 1) * 30, 3);

    public static decimal CalculateSuggestedQuantity(decimal expectedMonthlySales, decimal currentStock) =>
        Math.Max(expectedMonthlySales - currentStock, 0);
}
