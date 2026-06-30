using AutoPartsPOS.Application.Sales.Dtos;
using AutoPartsPOS.Application.Sales.Interfaces;
using AutoPartsPOS.Domain.Sales;
using AutoPartsPOS.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsPOS.Persistence.Sales;

public sealed class SalesInvoiceRepository(AppDbContext dbContext) : ISalesInvoiceRepository
{
    public async Task<IReadOnlyList<SalesInvoiceListDto>> SearchAsync(
        string? searchText,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SalesInvoices.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var term = searchText.Trim();
            query = query.Where(invoice =>
                EF.Functions.Like(invoice.InvoiceNumber, $"%{term}%") ||
                (invoice.Notes != null && EF.Functions.Like(invoice.Notes, $"%{term}%")));
        }

        if (fromDate is not null)
        {
            query = query.Where(invoice => invoice.InvoiceDate >= fromDate.Value);
        }

        if (toDate is not null)
        {
            var toDateExclusive = toDate.Value.AddDays(1);
            query = query.Where(invoice => invoice.InvoiceDate < toDateExclusive);
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
                GetStatusText(invoice.Status, invoice.PaymentStatus),
                invoice.SubtotalAmount,
                invoice.DiscountAmount,
                invoice.NetTotalAmount,
                invoice.PaidAmount,
                invoice.RemainingAmount,
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
            GetStatusText(invoice.Status, invoice.PaymentStatus),
            invoice.PaymentStatus,
            invoice.Status == SalesInvoiceStatus.Voided,
            invoice.SubtotalAmount,
            invoice.DiscountAmount,
            invoice.NetTotalAmount,
            invoice.PaidAmount,
            invoice.RemainingAmount,
            invoice.Notes,
            invoice.Items
                .OrderBy(item => item.Id)
                .Select(item => new SalesInvoiceItemDto(
                    item.Id,
                    item.ProductId,
                    item.Product?.NameAr ?? string.Empty,
                    item.Quantity,
                    item.UnitPrice,
                    item.TotalPrice,
                    item.UnitCost,
                    item.TotalCost))
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

    private static string GetStatusText(SalesInvoiceStatus status, InvoicePaymentStatus paymentStatus)
    {
        if (status == SalesInvoiceStatus.Voided)
        {
            return "ملغاة";
        }

        return paymentStatus switch
        {
            InvoicePaymentStatus.Paid => "مدفوعة بالكامل",
            InvoicePaymentStatus.PartiallyPaid => "مدفوعة جزئياً",
            InvoicePaymentStatus.Unpaid => "غير مدفوعة",
            _ => paymentStatus.ToString()
        };
    }
}
