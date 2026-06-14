using AutoPartsPOS.Application.Insights.Dtos;
using AutoPartsPOS.Application.Insights.Interfaces;
using AutoPartsPOS.Application.Insights.Services;
using AutoPartsPOS.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsPOS.Persistence.Reporting;

public sealed class InsightsRepository(AppDbContext dbContext) : IInsightsRepository
{
    public async Task<IReadOnlyList<LowStockInsightDto>> GetLowStockProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await dbContext.Products.AsNoTracking()
            .Where(product => product.IsActive && product.CurrentStock <= product.MinimumStock)
            .Select(product => new LowStockInsightDto(product.Id, product.ProductCode, product.NameAr, product.CurrentStock, product.MinimumStock))
            .ToListAsync(cancellationToken);

        return products
            .OrderBy(product => product.CurrentStock)
            .ThenBy(product => product.ProductNameAr)
            .ToList();
    }

    public async Task<IReadOnlyList<ReorderSuggestionDto>> GetReorderSuggestionsAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        var periodDays = Math.Max(toDate.DayNumber - fromDate.DayNumber + 1, 1);
        var soldLines = await dbContext.SalesInvoiceItems.AsNoTracking()
            .Where(item => item.SalesInvoice != null && item.SalesInvoice.Status == SalesInvoiceStatus.Posted && item.SalesInvoice.InvoiceDate >= fromDate && item.SalesInvoice.InvoiceDate <= toDate)
            .Select(item => new { item.ProductId, item.Quantity })
            .ToListAsync(cancellationToken);
        var soldQuantities = soldLines
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        var products = await dbContext.Products.AsNoTracking().Where(product => product.IsActive).OrderBy(product => product.NameAr).ToListAsync(cancellationToken);

        return products.Select(product =>
            {
                var expectedMonthlySales = SmartInsightCalculations.CalculateExpectedMonthlySales(soldQuantities.GetValueOrDefault(product.Id), periodDays);
                return new ReorderSuggestionDto(product.Id, product.ProductCode, product.NameAr, expectedMonthlySales, product.CurrentStock, SmartInsightCalculations.CalculateSuggestedQuantity(expectedMonthlySales, product.CurrentStock));
            })
            .Where(item => item.SuggestedQuantity > 0)
            .OrderByDescending(item => item.SuggestedQuantity)
            .ToList();
    }
}
