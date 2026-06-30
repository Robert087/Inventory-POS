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

    public ObservableCollection<ProductFilterOption> Products { get; } = [];

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private ProductFilterOption? _selectedProduct;

    [ObservableProperty]
    private DateTime? _fromDate;

    [ObservableProperty]
    private DateTime? _toDate;

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
        if (FromDate is not null && ToDate is not null && FromDate.Value.Date > ToDate.Value.Date)
        {
            ErrorMessage = "تاريخ البداية يجب ألا يكون بعد تاريخ النهاية.";
            return;
        }

        await LoadAsync();
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        SelectedProduct = Products.FirstOrDefault();
        FromDate = null;
        ToDate = null;
        await LoadAsync();
    }

    private async Task LoadProductsAsync(CancellationToken cancellationToken = default)
    {
        Products.Clear();
        Products.Add(new ProductFilterOption(null, "كل المنتجات"));

        foreach (var product in await productService.SearchAsync(null, null, cancellationToken))
        {
            Products.Add(new ProductFilterOption(product.Id, product.NameAr));
        }

        SelectedProduct = Products.FirstOrDefault();
    }

    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await ExecuteBusyAsync(async token =>
        {
            Transactions.Clear();
            var productId = SelectedProduct?.Id;
            DateOnly? fromDate = FromDate is null ? null : DateOnly.FromDateTime(FromDate.Value);
            DateOnly? toDate = ToDate is null ? null : DateOnly.FromDateTime(ToDate.Value);

            foreach (var transaction in await inventoryLedgerService.SearchAsync(null, productId, fromDate, toDate, token))
            {
                Transactions.Add(transaction);
            }
        }, cancellationToken);
    }

    public sealed record ProductFilterOption(long? Id, string NameAr);
}
