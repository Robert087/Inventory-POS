namespace AutoPartsPOS.Application.Dashboard.Dtos;

public sealed record MonthlyDashboardStatisticsDto(
    decimal Sales,
    int InvoiceCount,
    decimal NetProfit);
