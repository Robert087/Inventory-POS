using AutoPartsPOS.Application.Purchases.Dtos;
using AutoPartsPOS.WPF.Purchases.Dialogs;
using AutoPartsPOS.WPF.Purchases.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AutoPartsPOS.WPF.Purchases.Services;

public sealed class PurchaseDialogService(IServiceProvider serviceProvider) : IPurchaseDialogService
{
    public async Task<bool> ShowCreateDialogAsync()
    {
        var viewModel = serviceProvider.GetRequiredService<PurchaseInvoiceDialogViewModel>();
        await viewModel.LoadAsync();

        var dialog = new PurchaseInvoiceDialog
        {
            DataContext = viewModel,
            Owner = serviceProvider.GetRequiredService<MainWindow>()
        };

        viewModel.RequestClose += (_, result) => dialog.DialogResult = result;
        return dialog.ShowDialog() == true;
    }

    public Task ShowDetailsDialogAsync(PurchaseInvoiceDetailsDto invoice)
    {
        var viewModel = serviceProvider.GetRequiredService<PurchaseInvoiceDetailsViewModel>();
        viewModel.Load(invoice);

        var dialog = new PurchaseInvoiceDetailsDialog
        {
            DataContext = viewModel,
            Owner = serviceProvider.GetRequiredService<MainWindow>()
        };

        viewModel.RequestClose += (_, result) => dialog.DialogResult = result;
        dialog.ShowDialog();
        return Task.CompletedTask;
    }
}
