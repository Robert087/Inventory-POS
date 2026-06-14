using AutoPartsPOS.Application.Common.Models;
using AutoPartsPOS.Application.Sales.Dtos;

namespace AutoPartsPOS.Application.Sales.Interfaces;

public interface ISalesInvoiceService
{
    Task<IReadOnlyList<SalesInvoiceListDto>> SearchAsync(string? searchText, CancellationToken cancellationToken = default);

    Task<SalesInvoiceDetailsDto?> GetDetailsAsync(long id, CancellationToken cancellationToken = default);

    Task<OperationResult> CreateAsync(CreateSalesInvoiceDto dto, CancellationToken cancellationToken = default);

    Task<OperationResult> VoidAsync(long id, string? reason = null, CancellationToken cancellationToken = default);
}
