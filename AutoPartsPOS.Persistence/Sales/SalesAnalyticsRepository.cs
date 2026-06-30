using AutoPartsPOS.Application.Analytics.Dtos;
using AutoPartsPOS.Application.Analytics.Interfaces;
using AutoPartsPOS.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsPOS.Persistence.Sales;

public sealed class SalesAnalyticsRepository(AppDbContext dbContext) : ISalesAnalyticsRepository
{
    public async Task<IReadOnlyList<TopSellingProductDto>> GetTopSellingProductsAsync(DateOnly fromDate, DateOnly toDate, int take, CancellationToken cancellationToken = default)
    {
        var lines = await dbContext.SalesInvoiceItems
            .AsNoTracking()
            .Where(item =>
                item.SalesInvoice != null &&
                item.SalesInvoice.Status == SalesInvoiceStatus.Posted &&
                item.SalesInvoice.InvoiceDate >= fromDate &&
                item.SalesInvoice.InvoiceDate <= toDate)
            .Select(item => new
            {
                item.ProductId,
                ProductNameAr = item.Product == null ? string.Empty : item.Product.NameAr,
                item.Quantity,
                item.TotalPrice
            })
            .ToListAsync(cancellationToken);

        return lines
            .GroupBy(item => new { item.ProductId, item.ProductNameAr })
            .Select(group => new TopSellingProductDto(group.Key.ProductId, group.Key.ProductNameAr, group.Sum(item => item.Quantity), group.Sum(item => item.TotalPrice)))
            .OrderByDescending(item => item.QuantitySold)
            .Take(Math.Max(take, 1))
            .ToList();
    }

    public async Task<IReadOnlyList<SlowMovingProductDto>> GetSlowMovingProductsAsync(DateOnly cutoffDate, CancellationToken cancellationToken = default)
    {
        var lastSales = await dbContext.SalesInvoiceItems
            .AsNoTracking()
            .Where(item => item.SalesInvoice != null && item.SalesInvoice.Status == SalesInvoiceStatus.Posted)
            .GroupBy(item => item.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                LastSaleDate = group.Max(item => item.SalesInvoice!.InvoiceDate)
            })
            .ToDictionaryAsync(item => item.ProductId, item => item.LastSaleDate, cancellationToken);

        var products = await dbContext.Products
            .AsNoTracking()
            .Where(product => product.IsActive)
            .OrderBy(product => product.NameAr)
            .ToListAsync(cancellationToken);

        return products
            .Where(product => !lastSales.TryGetValue(product.Id, out var lastSaleDate) || lastSaleDate <= cutoffDate)
            .Select(product => new SlowMovingProductDto(
                product.Id,
                product.NameAr,
                product.CurrentStock,
                lastSales.TryGetValue(product.Id, out var lastSaleDate) ? lastSaleDate : null))
            .ToList();
    }

    public async Task<SalesSummaryDto> GetSalesSummaryAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        var invoices = await dbContext.SalesInvoices
            .AsNoTracking()
            .Where(invoice =>
                invoice.Status == SalesInvoiceStatus.Posted &&
                invoice.InvoiceDate >= fromDate &&
                invoice.InvoiceDate <= toDate)
            .Select(invoice => new
            {
                invoice.SubtotalAmount,
                invoice.DiscountAmount,
                invoice.NetTotalAmount
            })
            .ToListAsync(cancellationToken);

        return new SalesSummaryDto(
            invoices.Sum(invoice => invoice.SubtotalAmount),
            invoices.Sum(invoice => invoice.DiscountAmount),
            invoices.Sum(invoice => invoice.NetTotalAmount),
            invoices.Count);
    }
}
