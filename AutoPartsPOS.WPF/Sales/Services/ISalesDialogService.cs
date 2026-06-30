using AutoPartsPOS.Application.Sales.Dtos;

namespace AutoPartsPOS.WPF.Sales.Services;

public interface ISalesDialogService
{
    Task<bool> ShowCreateDialogAsync();

    Task<bool> ShowDetailsDialogAsync(SalesInvoiceDetailsDto invoice);

    Task<bool> ShowCancelConfirmationAsync();
}
