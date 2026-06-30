using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.WPF.Catalog.Dialogs;
using AutoPartsPOS.WPF.Catalog.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AutoPartsPOS.WPF.Catalog.Services;

public sealed class CatalogDialogService(IServiceProvider serviceProvider) : ICatalogDialogService
{
    public Task<bool> ShowCategoryDialogAsync(CategoryDto? category)
    {
        var viewModel = serviceProvider.GetRequiredService<CategoryDialogViewModel>();
        viewModel.Load(category);

        var dialog = new CategoryDialog
        {
            DataContext = viewModel,
            Owner = serviceProvider.GetRequiredService<MainWindow>()
        };

        viewModel.RequestClose += (_, result) => dialog.DialogResult = result;
        return Task.FromResult(dialog.ShowDialog() == true);
    }

    public async Task<bool> ShowProductDialogAsync(ProductDto? product)
    {
        var viewModel = serviceProvider.GetRequiredService<ProductDialogViewModel>();
        await viewModel.LoadAsync(product);

        var dialog = new ProductDialog
        {
            DataContext = viewModel,
            Owner = serviceProvider.GetRequiredService<MainWindow>()
        };

        viewModel.RequestClose += (_, result) => dialog.DialogResult = result;
        return dialog.ShowDialog() == true;
    }

    public Task<bool> ShowExistingProductConfirmationAsync(string productName)
    {
        var dialog = new ExistingProductConfirmationDialog(productName)
        {
            Owner = serviceProvider.GetRequiredService<MainWindow>()
        };

        return Task.FromResult(dialog.ShowDialog() == true);
    }

    public async Task<bool> ShowStockReplenishmentDialogAsync(ProductDto? product = null)
    {
        var viewModel = serviceProvider.GetRequiredService<StockReplenishmentDialogViewModel>();
        await viewModel.LoadAsync(product);

        var dialog = new StockReplenishmentDialog
        {
            DataContext = viewModel,
            Owner = serviceProvider.GetRequiredService<MainWindow>()
        };

        viewModel.RequestClose += (_, result) => dialog.DialogResult = result;
        return dialog.ShowDialog() == true;
    }
}
