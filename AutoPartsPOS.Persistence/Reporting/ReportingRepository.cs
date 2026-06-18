using AutoPartsPOS.Application.Reports.Dtos;
using AutoPartsPOS.Application.Reports.Interfaces;
using AutoPartsPOS.Application.Reports.Services;
using AutoPartsPOS.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsPOS.Persistence.Reporting;

public sealed class ReportingRepository(AppDbContext dbContext) : IReportingRepository
{
    public async Task<SalesReportDto> GetSalesReportAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        var invoices = await dbContext.SalesInvoices.AsNoTracking()
            .Where(invoice => invoice.Status == SalesInvoiceStatus.Posted && invoice.InvoiceDate >= fromDate && invoice.InvoiceDate <= toDate)
            .Select(invoice => new { invoice.SubtotalAmount, invoice.DiscountAmount, invoice.NetTotalAmount })
            .ToListAsync(cancellationToken);

        return new SalesReportDto(fromDate, toDate, invoices.Count, invoices.Sum(x => x.SubtotalAmount), invoices.Sum(x => x.DiscountAmount), invoices.Sum(x => x.NetTotalAmount));
    }

    public async Task<ProfitReportDto> GetProfitReportAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        var lines = await dbContext.SalesInvoiceItems.AsNoTracking()
            .Where(item => item.SalesInvoice != null && item.SalesInvoice.Status == SalesInvoiceStatus.Posted && item.SalesInvoice.InvoiceDate >= fromDate && item.SalesInvoice.InvoiceDate <= toDate)
            .Select(item => new { Revenue = item.TotalPrice, Cost = item.TotalCost })
            .ToListAsync(cancellationToken);
        var discounts = await dbContext.SalesInvoices.AsNoTracking()
            .Where(invoice => invoice.Status == SalesInvoiceStatus.Posted && invoice.InvoiceDate >= fromDate && invoice.InvoiceDate <= toDate)
            .Select(invoice => invoice.DiscountAmount)
            .ToListAsync(cancellationToken);
        var revenue = lines.Sum(line => line.Revenue) - discounts.Sum();
        var cost = lines.Sum(line => line.Cost);

        return new ProfitReportDto(fromDate, toDate, revenue, cost, ReportCalculations.CalculateProfit(revenue, cost));
    }

    public async Task<InventoryReportDto> GetInventoryReportAsync(CancellationToken cancellationToken = default)
    {
        var items = await dbContext.Products.AsNoTracking()
            .Where(product => product.IsActive)
            .OrderBy(product => product.NameAr)
            .Select(product => new InventoryReportItemDto(product.Id, product.ProductCode, product.NameAr, product.CurrentStock, product.CurrentAverageCost, product.CurrentStock * product.CurrentAverageCost, product.MinimumStock, product.CurrentStock <= product.MinimumStock))
            .ToListAsync(cancellationToken);

        return new InventoryReportDto(items.Sum(item => item.CurrentStock), items.Sum(item => item.InventoryValue), items.Count(item => item.IsLowStock), items);
    }
}
