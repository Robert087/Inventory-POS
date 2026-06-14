using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.Application.Catalog.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AutoPartsPOS.WPF.Catalog.ViewModels;

public sealed partial class ProductDialogViewModel(
    IProductService productService,
    ICategoryService categoryService) : ValidatableDialogViewModel
{
    private long? _productId;

    public ObservableCollection<CategoryLookupDto> Categories { get; } = [];

    [ObservableProperty]
    private string _dialogTitle = "إضافة منتج";

    [ObservableProperty]
    private string _productCode = string.Empty;

    [ObservableProperty]
    private string? _barcode;

    [ObservableProperty]
    private string _nameAr = string.Empty;

    [ObservableProperty]
    private long _categoryId;

    [ObservableProperty]
    private decimal _purchasePrice;

    [ObservableProperty]
    private decimal _sellingPrice;

    [ObservableProperty]
    private decimal _currentStock;

    [ObservableProperty]
    private decimal _minimumStock;

    [ObservableProperty]
    private bool _isActive = true;

    public async Task LoadAsync(ProductDto? product, CancellationToken cancellationToken = default)
    {
        Categories.Clear();

        foreach (var category in await categoryService.GetActiveLookupAsync(cancellationToken))
        {
            Categories.Add(category);
        }

        _productId = product?.Id;
        DialogTitle = product is null ? "إضافة منتج" : "تعديل منتج";
        ProductCode = product?.ProductCode ?? string.Empty;
        Barcode = product?.Barcode;
        NameAr = product?.NameAr ?? string.Empty;
        CategoryId = product?.CategoryId ?? Categories.FirstOrDefault()?.Id ?? 0;
        PurchasePrice = product?.PurchasePrice ?? 0;
        SellingPrice = product?.SellingPrice ?? 0;
        CurrentStock = product?.CurrentStock ?? 0;
        MinimumStock = product?.MinimumStock ?? 0;
        IsActive = product?.IsActive ?? true;
        ApplyErrors(new Dictionary<string, string[]>());
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await ExecuteBusyAsync(async cancellationToken =>
        {
            var result = await productService.SaveAsync(new SaveProductDto
            {
                Id = _productId,
                ProductCode = ProductCode,
                Barcode = Barcode,
                NameAr = NameAr,
                CategoryId = CategoryId,
                PurchasePrice = PurchasePrice,
                SellingPrice = SellingPrice,
                CurrentStock = CurrentStock,
                MinimumStock = MinimumStock,
                IsActive = IsActive
            }, cancellationToken);

            if (!result.Succeeded)
            {
                ApplyErrors(result.Errors);
                ErrorMessage = result.ErrorSummary;
                return;
            }

            Close(true);
        });
    }

    [RelayCommand]
    private void Cancel()
    {
        Close(false);
    }
}
