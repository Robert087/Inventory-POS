using AutoPartsPOS.Application.Purchases.Dtos;
using AutoPartsPOS.Application.Purchases.Interfaces;
using AutoPartsPOS.Domain.Purchases;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsPOS.Persistence.Purchases;

public sealed class PurchaseInvoiceRepository(AppDbContext dbContext) : IPurchaseInvoiceRepository
{
    public async Task<IReadOnlyList<PurchaseInvoiceListDto>> SearchAsync(string? searchText, CancellationToken cancellationToken = default)
    {
        var query = dbContext.PurchaseInvoices
            .AsNoTracking()
            .Include(invoice => invoice.Supplier)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var term = searchText.Trim();
            query = query.Where(invoice =>
                EF.Functions.Like(invoice.InvoiceNumber, $"%{term}%") ||
                (invoice.Supplier != null && EF.Functions.Like(invoice.Supplier.NameAr, $"%{term}%")) ||
                (invoice.Notes != null && EF.Functions.Like(invoice.Notes, $"%{term}%")));
        }

        var invoices = await query
            .OrderByDescending(invoice => invoice.InvoiceDate)
            .ThenByDescending(invoice => invoice.Id)
            .ToListAsync(cancellationToken);

        return invoices
            .Select(invoice => new PurchaseInvoiceListDto(
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.SupplierId,
                invoice.Supplier?.NameAr ?? string.Empty,
                invoice.InvoiceDate,
                GetStatusText(invoice.Status),
                invoice.TotalAmount,
                invoice.Notes))
            .ToList();
    }

    public async Task<PurchaseInvoiceDetailsDto?> GetDetailsAsync(long id, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.PurchaseInvoices
            .AsNoTracking()
            .Include(item => item.Supplier)
            .Include(item => item.Items)
            .ThenInclude(item => item.Product)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (invoice is null)
        {
            return null;
        }

        return new PurchaseInvoiceDetailsDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.SupplierId,
            invoice.Supplier?.NameAr ?? string.Empty,
            invoice.InvoiceDate,
            GetStatusText(invoice.Status),
            invoice.TotalAmount,
            invoice.Notes,
            invoice.Items
                .OrderBy(item => item.Id)
                .Select(item => new PurchaseInvoiceItemDto(
                    item.Id,
                    item.ProductId,
                    item.Product?.NameAr ?? string.Empty,
                    item.Quantity,
                    item.UnitPrice,
                    item.TotalPrice))
                .ToList());
    }

    public Task<PurchaseInvoice?> GetByIdWithItemsAsync(long id, CancellationToken cancellationToken = default)
    {
        return dbContext.PurchaseInvoices
            .Include(invoice => invoice.Items)
            .SingleOrDefaultAsync(invoice => invoice.Id == id, cancellationToken);
    }

    public Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        return dbContext.PurchaseInvoices
            .AnyAsync(invoice => invoice.InvoiceNumber == invoiceNumber, cancellationToken);
    }

    public Task AddAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default)
    {
        return dbContext.PurchaseInvoices.AddAsync(invoice, cancellationToken).AsTask();
    }

    private static string GetStatusText(PurchaseInvoiceStatus status)
    {
        return status switch
        {
            PurchaseInvoiceStatus.Posted => "Ù…Ø±Ø­Ù„Ø©",
            PurchaseInvoiceStatus.Voided => "Ù…Ù„ØºØ§Ø©",
            _ => status.ToString()
        };
    }
}

