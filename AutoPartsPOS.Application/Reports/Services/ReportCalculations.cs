namespace AutoPartsPOS.Application.Reports.Services;

public static class ReportCalculations
{
    public static decimal CalculateProfit(decimal revenue, decimal cost) => revenue - cost;
}
