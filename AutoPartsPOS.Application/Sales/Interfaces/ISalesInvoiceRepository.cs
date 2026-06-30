using AutoPartsPOS.Application.Sales.Dtos;
using AutoPartsPOS.Domain.Sales;

namespace AutoPartsPOS.Application.Sales.Interfaces;

public interface ISalesInvoiceRepository
{
    Task<IReadOnlyList<SalesInvoiceListDto>> SearchAsync(
        string? searchText,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);

    Task<SalesInvoiceDetailsDto?> GetDetailsAsync(long id, CancellationToken cancellationToken = default);

    Task<SalesInvoice?> GetByIdWithItemsAsync(long id, CancellationToken cancellationToken = default);

    Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, CancellationToken cancellationToken = default);

    Task AddAsync(SalesInvoice invoice, CancellationToken cancellationToken = default);
}
