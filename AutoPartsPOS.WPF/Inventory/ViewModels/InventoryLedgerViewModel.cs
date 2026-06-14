using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.Application.Catalog.Interfaces;
using AutoPartsPOS.Application.Common.ViewModels;
using AutoPartsPOS.Application.Inventory.Dtos;
using AutoPartsPOS.Application.Inventory.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AutoPartsPOS.WPF.Inventory.ViewModels;

public sealed partial class InventoryLedgerViewModel(
    IInventoryLedgerService inventoryLedgerService,
    IProductService productService) : ViewModelBase
{
    public ObservableCollection<InventoryTransactionDto> Transactions { get; } = [];

    public ObservableCollection<ProductDto> Products { get; } = [];

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private ProductDto? _selectedProduct;

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Title = "حركة المخزون";

        try
        {
            await LoadProductsAsync(cancellationToken);
            await LoadAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            ErrorMessage = $"تعذر تحميل حركة المخزون: {exception.Message}";
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadAsync();
    }

    private async Task LoadProductsAsync(CancellationToken cancellationToken = default)
    {
        Products.Clear();

        foreach (var product in await productService.SearchAsync(null, null, cancellationToken))
        {
            Products.Add(product);
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await ExecuteBusyAsync(async token =>
        {
            Transactions.Clear();
            var productId = SelectedProduct?.Id;

            foreach (var transaction in await inventoryLedgerService.SearchAsync(SearchText, productId, token))
            {
                Transactions.Add(transaction);
            }
        }, cancellationToken);
    }
}
