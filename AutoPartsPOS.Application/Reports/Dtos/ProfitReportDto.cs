namespace AutoPartsPOS.Application.Reports.Dtos;

public sealed record ProfitReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    decimal Revenue,
    decimal Cost,
    decimal Profit);
