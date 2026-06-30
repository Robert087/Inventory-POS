using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.Application.Catalog.Interfaces;
using AutoPartsPOS.WPF.Catalog.Services;
using AutoPartsPOS.WPF.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AutoPartsPOS.WPF.Catalog.ViewModels;

public sealed partial class ProductDialogViewModel(
    IProductService productService,
    ICategoryService categoryService,
    ICatalogDialogService catalogDialogService) : ValidatableDialogViewModel
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
    private string? _purchasePriceText;

    [ObservableProperty]
    private string _purchasePriceHint = "سعر الشراء";

    public bool IsEditMode => _productId is not null;

    [ObservableProperty]
    private string? _sellingPriceText;

    [ObservableProperty]
    private string? _currentStockText;

    [ObservableProperty]
    private string? _minimumStockText;

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
        PurchasePriceHint = product is null ? "سعر الشراء" : "متوسط سعر الشراء";
        ProductCode = product?.ProductCode ?? string.Empty;
        Barcode = product?.Barcode;
        NameAr = product?.NameAr ?? string.Empty;
        CategoryId = product?.CategoryId ?? Categories.FirstOrDefault()?.Id ?? 0;
        PurchasePriceText = product is null
            ? null
            : WholeNumberInput.Format(product.CurrentAverageCost);
        SellingPriceText = product is null || product.SellingPrice == 0 ? null : WholeNumberInput.Format(product.SellingPrice);
        CurrentStockText = product is null ? null : WholeNumberInput.Format(product.CurrentStock);
        MinimumStockText = product is null ? null : WholeNumberInput.Format(product.MinimumStock);
        IsActive = product?.IsActive ?? true;
        ApplyErrors(new Dictionary<string, string[]>());
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var errors = new Dictionary<string, string[]>();
        var purchasePrice = 0m;

        if (_productId is null)
        {
            if (!WholeNumberInput.TryParseRequiredPositive(
                    PurchasePriceText,
                    out purchasePrice,
                    out var purchasePriceError,
                    "سعر الشراء مطلوب",
                    "سعر الشراء يجب أن يكون أكبر من صفر",
                    "سعر الشراء يجب أن يكون عدداً صحيحاً"))
            {
                errors[nameof(PurchasePriceText)] = [purchasePriceError!];
            }
        }
        else if (!WholeNumberInput.TryParseOptional(PurchasePriceText, out purchasePrice))
        {
            errors[nameof(PurchasePriceText)] = ["متوسط سعر الشراء يجب أن يكون عدداً صحيحاً."];
        }

        if (!WholeNumberInput.TryParseOptional(SellingPriceText, out var sellingPrice))
        {
            errors[nameof(SellingPriceText)] = ["سعر البيع يجب أن يكون عدداً صحيحاً."];
        }

        if (!WholeNumberInput.TryParseRequired(
                CurrentStockText,
                out var currentStock,
                out var currentStockError,
                "المخزون الحالي مطلوب",
                "المخزون الحالي يجب أن يكون عدداً صحيحاً"))
        {
            errors[nameof(CurrentStockText)] = [currentStockError!];
        }
        else if (currentStock < 0)
        {
            errors[nameof(CurrentStockText)] = ["المخزون الحالي لا يمكن أن يكون أقل من صفر."];
        }

        if (!WholeNumberInput.TryParseRequired(
                MinimumStockText,
                out var minimumStock,
                out var minimumStockError,
                "الحد الأدنى للمخزون مطلوب",
                "الحد الأدنى للمخزون يجب أن يكون عدداً صحيحاً"))
        {
            errors[nameof(MinimumStockText)] = [minimumStockError!];
        }
        else if (minimumStock < 0)
        {
            errors[nameof(MinimumStockText)] = ["الحد الأدنى للمخزون لا يمكن أن يكون أقل من صفر."];
        }

        if (errors.Count > 0)
        {
            ApplyErrors(errors);
            ErrorMessage = errors.Values.SelectMany(messages => messages).FirstOrDefault();
            return;
        }

        await ExecuteBusyAsync(async cancellationToken =>
        {
            if (_productId is null)
            {
                var existingProduct = await productService.GetByProductCodeAsync(ProductCode, cancellationToken);

                if (existingProduct is not null)
                {
                    if (await catalogDialogService.ShowExistingProductConfirmationAsync(existingProduct.NameAr))
                    {
                        if (await catalogDialogService.ShowStockReplenishmentDialogAsync(existingProduct))
                        {
                            Close(true);
                        }
                    }

                    return;
                }
            }

            var result = await productService.SaveAsync(new SaveProductDto
            {
                Id = _productId,
                ProductCode = ProductCode,
                Barcode = Barcode,
                NameAr = NameAr,
                CategoryId = CategoryId,
                PurchasePrice = _productId is null ? purchasePrice : 0,
                SellingPrice = sellingPrice,
                CurrentStock = currentStock,
                MinimumStock = minimumStock,
                IsActive = IsActive
            }, cancellationToken);

            if (!result.Succeeded)
            {
                if (_productId is null &&
                    result.Errors.TryGetValue(nameof(SaveProductDto.ProductCode), out var codeErrors) &&
                    codeErrors.Any(message => message.Contains("مستخدم", StringComparison.Ordinal)))
                {
                    var existingProduct = await productService.GetByProductCodeAsync(ProductCode, cancellationToken);

                    if (existingProduct is not null)
                    {
                        if (await catalogDialogService.ShowExistingProductConfirmationAsync(existingProduct.NameAr))
                        {
                            if (await catalogDialogService.ShowStockReplenishmentDialogAsync(existingProduct))
                            {
                                Close(true);
                            }
                        }

                        return;
                    }
                }

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
