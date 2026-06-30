using AutoPartsPOS.Application.Sales.Dtos;
using AutoPartsPOS.WPF.Sales.Dialogs;
using AutoPartsPOS.WPF.Sales.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AutoPartsPOS.WPF.Sales.Services;

public sealed class SalesDialogService(IServiceProvider serviceProvider) : ISalesDialogService
{
    public async Task<bool> ShowCreateDialogAsync()
    {
        var viewModel = serviceProvider.GetRequiredService<SalesInvoiceDialogViewModel>();
        await viewModel.LoadAsync();

        var dialog = new SalesInvoiceDialog
        {
            DataContext = viewModel,
            Owner = serviceProvider.GetRequiredService<MainWindow>()
        };

        viewModel.RequestClose += (_, result) => dialog.DialogResult = result;
        return dialog.ShowDialog() == true;
    }

    public Task<bool> ShowDetailsDialogAsync(SalesInvoiceDetailsDto invoice)
    {
        var viewModel = serviceProvider.GetRequiredService<SalesInvoiceDetailsViewModel>();
        viewModel.Load(invoice);

        var dialog = new SalesInvoiceDetailsDialog
        {
            DataContext = viewModel,
            Owner = serviceProvider.GetRequiredService<MainWindow>()
        };

        viewModel.RequestClose += (_, result) => dialog.DialogResult = result;
        return Task.FromResult(dialog.ShowDialog() == true);
    }

    public Task<bool> ShowCancelConfirmationAsync()
    {
        var dialog = new SalesInvoiceCancelConfirmationDialog
        {
            Owner = serviceProvider.GetRequiredService<MainWindow>()
        };

        return Task.FromResult(dialog.ShowDialog() == true);
    }
}
