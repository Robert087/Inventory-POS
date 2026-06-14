namespace AutoPartsPOS.Application.Reports.Dtos;

public sealed record SalesReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int InvoiceCount,
    decimal TotalSales,
    decimal TotalDiscount,
    decimal NetSales);
