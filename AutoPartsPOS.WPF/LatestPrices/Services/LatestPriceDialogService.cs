using AutoPartsPOS.Application.LatestPrices.Dtos;
using AutoPartsPOS.WPF.LatestPrices.Dialogs;
using AutoPartsPOS.WPF.LatestPrices.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AutoPartsPOS.WPF.LatestPrices.Services;

public sealed class LatestPriceDialogService(IServiceProvider serviceProvider) : ILatestPriceDialogService
{
    public async Task<bool> ShowLatestPriceDialogAsync(LatestPriceDto? latestPrice)
    {
        var viewModel = serviceProvider.GetRequiredService<LatestPriceDialogViewModel>();
        await viewModel.LoadAsync(latestPrice);

        var dialog = new LatestPriceDialog
        {
            DataContext = viewModel,
            Owner = serviceProvider.GetRequiredService<MainWindow>()
        };

        viewModel.RequestClose += (_, result) => dialog.DialogResult = result;
        return dialog.ShowDialog() == true;
    }
}
