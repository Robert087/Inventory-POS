using AutoPartsPOS.Application.Common.Models;
using AutoPartsPOS.Application.Sales.Dtos;
using AutoPartsPOS.Domain.Common;

namespace AutoPartsPOS.Application.Sales.Interfaces;

public interface ISalesInvoiceService
{
    Task<IReadOnlyList<SalesInvoiceListDto>> SearchAsync(
        string? searchText,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);

    Task<SalesInvoiceDetailsDto?> GetDetailsAsync(long id, CancellationToken cancellationToken = default);

    Task<OperationResult> CreateAsync(CreateSalesInvoiceDto dto, CancellationToken cancellationToken = default);

    Task<OperationResult> UpdatePaymentAsync(long id, InvoicePaymentStatus paymentStatus, decimal paidAmount, CancellationToken cancellationToken = default);

    Task<OperationResult> VoidAsync(long id, string? reason = null, CancellationToken cancellationToken = default);
}
