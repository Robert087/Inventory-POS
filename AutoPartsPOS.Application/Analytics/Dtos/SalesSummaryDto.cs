namespace AutoPartsPOS.Application.Analytics.Dtos;

public sealed record SalesSummaryDto(decimal GrossSales, decimal DiscountAmount, decimal NetSales, int InvoiceCount);
