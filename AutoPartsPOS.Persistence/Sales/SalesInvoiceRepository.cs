using AutoPartsPOS.Application.Sales.Dtos;
using AutoPartsPOS.Application.Sales.Interfaces;
using AutoPartsPOS.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsPOS.Persistence.Sales;

public sealed class SalesInvoiceRepository(AppDbContext dbContext) : ISalesInvoiceRepository
{
    public async Task<IReadOnlyList<SalesInvoiceListDto>> SearchAsync(string? searchText, CancellationToken cancellationToken = default)
    {
        var query = dbContext.SalesInvoices.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var term = searchText.Trim();
            query = query.Where(invoice =>
                EF.Functions.Like(invoice.InvoiceNumber, $"%{term}%") ||
                (invoice.Notes != null && EF.Functions.Like(invoice.Notes, $"%{term}%")));
        }

        var invoices = await query
            .OrderByDescending(invoice => invoice.InvoiceDate)
            .ThenByDescending(invoice => invoice.Id)
            .ToListAsync(cancellationToken);

        return invoices
            .Select(invoice => new SalesInvoiceListDto(
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.InvoiceDate,
                GetStatusText(invoice.Status),
                invoice.SubtotalAmount,
                invoice.DiscountAmount,
                invoice.NetTotalAmount,
                invoice.Notes))
            .ToList();
    }

    public async Task<SalesInvoiceDetailsDto?> GetDetailsAsync(long id, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.SalesInvoices
            .AsNoTracking()
            .Include(item => item.Items)
            .ThenInclude(item => item.Product)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (invoice is null)
        {
            return null;
        }

        return new SalesInvoiceDetailsDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.InvoiceDate,
            GetStatusText(invoice.Status),
            invoice.SubtotalAmount,
            invoice.DiscountAmount,
            invoice.NetTotalAmount,
            invoice.Notes,
            invoice.Items
                .OrderBy(item => item.Id)
                .Select(item => new SalesInvoiceItemDto(
                    item.Id,
                    item.ProductId,
                    item.Product?.NameAr ?? string.Empty,
                    item.Quantity,
                    item.UnitPrice,
                    item.TotalPrice))
                .ToList());
    }

    public Task<SalesInvoice?> GetByIdWithItemsAsync(long id, CancellationToken cancellationToken = default)
    {
        return dbContext.SalesInvoices
            .Include(invoice => invoice.Items)
            .SingleOrDefaultAsync(invoice => invoice.Id == id, cancellationToken);
    }

    public Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        return dbContext.SalesInvoices
            .AnyAsync(invoice => invoice.InvoiceNumber == invoiceNumber, cancellationToken);
    }

    public Task AddAsync(SalesInvoice invoice, CancellationToken cancellationToken = default)
    {
        return dbContext.SalesInvoices.AddAsync(invoice, cancellationToken).AsTask();
    }

    private static string GetStatusText(SalesInvoiceStatus status)
    {
        return status switch
        {
            SalesInvoiceStatus.Posted => "Ù…Ø±Ø­Ù„Ø©",
            SalesInvoiceStatus.Voided => "Ù…Ù„ØºØ§Ø©",
            _ => status.ToString()
        };
    }
}

