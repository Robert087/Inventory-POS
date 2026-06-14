using AutoPartsPOS.Application.Common.Models;
using AutoPartsPOS.Application.Purchases.Dtos;

namespace AutoPartsPOS.Application.Purchases.Interfaces;

public interface IPurchaseInvoiceService
{
    Task<IReadOnlyList<PurchaseInvoiceListDto>> SearchAsync(string? searchText, CancellationToken cancellationToken = default);

    Task<PurchaseInvoiceDetailsDto?> GetDetailsAsync(long id, CancellationToken cancellationToken = default);

    Task<OperationResult> CreateAsync(CreatePurchaseInvoiceDto dto, CancellationToken cancellationToken = default);

    Task<OperationResult> VoidAsync(long id, string? reason = null, CancellationToken cancellationToken = default);
}
