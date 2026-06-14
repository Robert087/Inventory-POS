using AutoPartsPOS.Application.Purchases.Dtos;
using AutoPartsPOS.Domain.Purchases;

namespace AutoPartsPOS.Application.Purchases.Interfaces;

public interface IPurchaseInvoiceRepository
{
    Task<IReadOnlyList<PurchaseInvoiceListDto>> SearchAsync(string? searchText, CancellationToken cancellationToken = default);

    Task<PurchaseInvoiceDetailsDto?> GetDetailsAsync(long id, CancellationToken cancellationToken = default);

    Task<PurchaseInvoice?> GetByIdWithItemsAsync(long id, CancellationToken cancellationToken = default);

    Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, CancellationToken cancellationToken = default);

    Task AddAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default);
}
