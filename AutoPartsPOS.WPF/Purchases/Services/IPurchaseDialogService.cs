using AutoPartsPOS.Application.Purchases.Dtos;

namespace AutoPartsPOS.WPF.Purchases.Services;

public interface IPurchaseDialogService
{
    Task<bool> ShowCreateDialogAsync();

    Task ShowDetailsDialogAsync(PurchaseInvoiceDetailsDto invoice);
}
