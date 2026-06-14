using AutoPartsPOS.Application.Suppliers.Dtos;
using AutoPartsPOS.WPF.Suppliers.Dialogs;
using AutoPartsPOS.WPF.Suppliers.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AutoPartsPOS.WPF.Suppliers.Services;

public sealed class SupplierDialogService(IServiceProvider serviceProvider) : ISupplierDialogService
{
    public Task<bool> ShowSupplierDialogAsync(SupplierDto? supplier)
    {
        var viewModel = serviceProvider.GetRequiredService<SupplierDialogViewModel>();
        viewModel.Load(supplier);

        var dialog = new SupplierDialog
        {
            DataContext = viewModel,
            Owner = serviceProvider.GetRequiredService<MainWindow>()
        };

        viewModel.RequestClose += (_, result) => dialog.DialogResult = result;
        return Task.FromResult(dialog.ShowDialog() == true);
    }
}
