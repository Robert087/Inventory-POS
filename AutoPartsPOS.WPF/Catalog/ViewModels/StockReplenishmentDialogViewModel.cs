using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.Application.Catalog.Interfaces;
using AutoPartsPOS.WPF.Helpers;
using AutoPartsPOS.WPF.Catalog.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AutoPartsPOS.WPF.Catalog.ViewModels;

public sealed partial class StockReplenishmentDialogViewModel(IProductService productService) : ValidatableDialogViewModel
{
    private long _productId;
    private bool _isSynchronizing;
    private IReadOnlyList<ProductDto> _allProducts = [];

    public ObservableCollection<ProductDto> FilteredNameProducts { get; } = [];

    public ObservableCollection<ProductDto> FilteredCodeProducts { get; } = [];

    [ObservableProperty]
    private string _nameSearchText = string.Empty;

    [ObservableProperty]
    private string _codeSearchText = string.Empty;

    [ObservableProperty]
    private string _nameAr = string.Empty;

    [ObservableProperty]
    private string _productCode = string.Empty;

    [ObservableProperty]
    private string _currentStockText = string.Empty;

    [ObservableProperty]
    private string _currentAverageCostText = string.Empty;

    [ObservableProperty]
    private string _latestPurchasePriceText = string.Empty;

    [ObservableProperty]
    private string? _newQuantityText;

    [ObservableProperty]
    private string? _newPurchasePriceText;

    [ObservableProperty]
    private bool _hasSelectedProduct;

    public async Task LoadAsync(ProductDto? product = null, CancellationToken cancellationToken = default)
    {
        _allProducts = await productService.SearchAsync(null, null, cancellationToken);
        ApplyNameFilter(string.Empty);
        ApplyCodeFilter(string.Empty);
        NewQuantityText = null;
        NewPurchasePriceText = null;
        ApplyErrors(new Dictionary<string, string[]>());

        if (product is not null)
        {
            var current = await productService.GetByIdAsync(product.Id, cancellationToken) ?? product;
            SelectProduct(current);
            return;
        }

        ClearProductSelection();
    }

    public void OnProductSelected(ProductDto product)
    {
        SelectProduct(product);
    }

    partial void OnNameSearchTextChanged(string value)
    {
        if (_isSynchronizing)
        {
            return;
        }

        ApplyNameFilter(value);
    }

    partial void OnCodeSearchTextChanged(string value)
    {
        if (_isSynchronizing)
        {
            return;
        }

        ApplyCodeFilter(value);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var errors = new Dictionary<string, string[]>();

        if (_productId <= 0)
        {
            errors[string.Empty] = ["يرجى اختيار صنف أولاً."];
        }

        if (!WholeNumberInput.TryParseRequiredPositive(
                NewQuantityText,
                out var quantity,
                out var quantityError,
                "الكمية الجديدة مطلوبة",
                "الكمية الجديدة يجب أن تكون أكبر من صفر",
                "الكمية الجديدة يجب أن تكون عدداً صحيحاً"))
        {
            errors[nameof(NewQuantityText)] = [quantityError!];
        }

        if (!WholeNumberInput.TryParseRequiredPositive(
                NewPurchasePriceText,
                out var purchasePrice,
                out var purchasePriceError,
                "سعر الشراء الجديد مطلوب",
                "سعر الشراء الجديد يجب أن يكون أكبر من صفر",
                "سعر الشراء الجديد يجب أن يكون عدداً صحيحاً"))
        {
            errors[nameof(NewPurchasePriceText)] = [purchasePriceError!];
        }

        if (errors.Count > 0)
        {
            ApplyErrors(errors);
            ErrorMessage = errors.TryGetValue(string.Empty, out var generalErrors)
                ? generalErrors[0]
                : "يرجى تصحيح الحقول المطلوبة.";
            return;
        }

        await ExecuteBusyAsync(async cancellationToken =>
        {
            var result = await productService.ReplenishStockAsync(new ReplenishProductStockDto
            {
                ProductId = _productId,
                Quantity = quantity,
                PurchasePrice = purchasePrice
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

    private void SelectProduct(ProductDto product)
    {
        _isSynchronizing = true;

        try
        {
            _productId = product.Id;
            NameSearchText = product.NameAr;
            CodeSearchText = product.ProductCode;
            ApplyNameFilter(product.NameAr);
            ApplyCodeFilter(product.ProductCode);
            UpdateReadOnlyFields(product);
            HasSelectedProduct = true;
            ErrorMessage = null;
        }
        finally
        {
            _isSynchronizing = false;
        }
    }

    private void ClearProductSelection()
    {
        _isSynchronizing = true;

        try
        {
            _productId = 0;
            NameSearchText = string.Empty;
            CodeSearchText = string.Empty;
            NameAr = string.Empty;
            ProductCode = string.Empty;
            CurrentStockText = string.Empty;
            CurrentAverageCostText = string.Empty;
            LatestPurchasePriceText = string.Empty;
            HasSelectedProduct = false;
        }
        finally
        {
            _isSynchronizing = false;
        }
    }

    private void UpdateReadOnlyFields(ProductDto product)
    {
        NameAr = product.NameAr;
        ProductCode = product.ProductCode;
        CurrentStockText = WholeNumberInput.Format(product.CurrentStock);
        CurrentAverageCostText = WholeNumberInput.Format(product.CurrentAverageCost);
        LatestPurchasePriceText = WholeNumberInput.Format(product.PurchasePrice);
    }

    private void ApplyNameFilter(string? searchText)
    {
        ReplaceCollection(FilteredNameProducts, FilterProducts(searchText, product => product.NameAr));
    }

    private void ApplyCodeFilter(string? searchText)
    {
        ReplaceCollection(FilteredCodeProducts, FilterProducts(searchText, product => product.ProductCode));
    }

    private IEnumerable<ProductDto> FilterProducts(string? searchText, Func<ProductDto, string> selector)
    {
        var activeProducts = _allProducts.Where(product => product.IsActive);

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return activeProducts.OrderBy(product => product.NameAr);
        }

        var term = searchText.Trim();
        return activeProducts
            .Where(product => selector(product).Contains(term, StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(product => product.NameAr);
    }

    private static void ReplaceCollection(ObservableCollection<ProductDto> collection, IEnumerable<ProductDto> values)
    {
        collection.Clear();

        foreach (var value in values)
        {
            collection.Add(value);
        }
    }
}
