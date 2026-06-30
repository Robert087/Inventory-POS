using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.Application.Catalog.Interfaces;
using AutoPartsPOS.Application.LatestPrices.Dtos;
using AutoPartsPOS.Application.LatestPrices.Interfaces;
using AutoPartsPOS.WPF.Helpers;
using AutoPartsPOS.WPF.Catalog.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AutoPartsPOS.WPF.LatestPrices.ViewModels;

public sealed partial class LatestPriceDialogViewModel(
    ILatestPriceService latestPriceService,
    IProductService productService) : ValidatableDialogViewModel
{
    private long? _productId;
    private bool _isSynchronizing;
    private IReadOnlyList<ProductDto> _allProducts = [];

    public ObservableCollection<ProductDto> FilteredNameProducts { get; } = [];

    public ObservableCollection<ProductDto> FilteredCodeProducts { get; } = [];

    [ObservableProperty]
    private string _dialogTitle = "إضافة سعر";

    [ObservableProperty]
    private string _codeSearchText = string.Empty;

    [ObservableProperty]
    private string _nameSearchText = string.Empty;

    [ObservableProperty]
    private string _registeredLatestPriceText = "—";

    [ObservableProperty]
    private string? _latestPurchasePriceText;

    [ObservableProperty]
    private bool _hasSelectedProduct;

    public async Task LoadAsync(LatestPriceDto? latestPrice, CancellationToken cancellationToken = default)
    {
        _allProducts = await productService.SearchAsync(null, null, cancellationToken);
        ApplyNameFilter(string.Empty);
        ApplyCodeFilter(string.Empty);
        LatestPurchasePriceText = null;
        ApplyErrors(new Dictionary<string, string[]>());

        _productId = latestPrice?.Id;
        DialogTitle = latestPrice is null ? "إضافة سعر" : "تعديل سعر";

        if (latestPrice is not null)
        {
            var product = _allProducts.FirstOrDefault(item => item.Id == latestPrice.Id)
                ?? await productService.GetByIdAsync(latestPrice.Id, cancellationToken);

            if (product is not null)
            {
                ApplyProductSelection(product);
                return;
            }

            _isSynchronizing = true;
            CodeSearchText = latestPrice.ProductCode;
            NameSearchText = latestPrice.NameAr;
            RegisteredLatestPriceText = latestPrice.LatestPurchasePrice > 0
                ? WholeNumberInput.Format(latestPrice.LatestPurchasePrice)
                : "—";
            HasSelectedProduct = true;
            _isSynchronizing = false;
            return;
        }

        ClearProductSelection();
    }

    public void OnProductSelected(ProductDto product)
    {
        ApplyProductSelection(product);
    }

    partial void OnNameSearchTextChanged(string value)
    {
        if (_isSynchronizing)
        {
            return;
        }

        ApplyNameFilter(value);
        InvalidateSelectionIfTextChanged();
    }

    partial void OnCodeSearchTextChanged(string value)
    {
        if (_isSynchronizing)
        {
            return;
        }

        ApplyCodeFilter(value);
        InvalidateSelectionIfTextChanged();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var errors = new Dictionary<string, string[]>();

        if (!HasSelectedProduct || _productId is null or <= 0)
        {
            errors[string.Empty] = ["يرجى اختيار صنف صحيح"];
        }

        if (!WholeNumberInput.TryParseRequiredPositive(
                LatestPurchasePriceText,
                out var latestPurchasePrice,
                out var latestPurchasePriceError,
                "أحدث سعر شراء مطلوب",
                "أحدث سعر شراء يجب أن يكون أكبر من صفر",
                "أحدث سعر شراء يجب أن يكون عدداً صحيحاً"))
        {
            errors[nameof(LatestPurchasePriceText)] = [latestPurchasePriceError!];
        }

        if (errors.Count > 0)
        {
            ApplyErrors(errors);
            ErrorMessage = errors.TryGetValue(string.Empty, out var generalErrors)
                ? generalErrors[0]
                : "يرجى تصحيح الحقول المطلوبة.";
            return;
        }

        var selectedProduct = _allProducts.First(product => product.Id == _productId);

        await ExecuteBusyAsync(async cancellationToken =>
        {
            var result = await latestPriceService.SaveAsync(new SaveLatestPriceDto
            {
                Id = _productId,
                ProductCode = selectedProduct.ProductCode,
                NameAr = selectedProduct.NameAr,
                LatestPurchasePrice = latestPurchasePrice
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

    private void ApplyProductSelection(ProductDto product)
    {
        _isSynchronizing = true;

        try
        {
            _productId = product.Id;
            CodeSearchText = product.ProductCode;
            NameSearchText = product.NameAr;
            ApplyNameFilter(product.NameAr);
            ApplyCodeFilter(product.ProductCode);
            RegisteredLatestPriceText = product.PurchasePrice > 0
                ? WholeNumberInput.Format(product.PurchasePrice)
                : "—";
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
            _productId = null;
            CodeSearchText = string.Empty;
            NameSearchText = string.Empty;
            RegisteredLatestPriceText = "—";
            HasSelectedProduct = false;
        }
        finally
        {
            _isSynchronizing = false;
        }
    }

    private void InvalidateSelectionIfTextChanged()
    {
        if (_productId is null or <= 0)
        {
            HasSelectedProduct = false;
            RegisteredLatestPriceText = "—";
            return;
        }

        var product = _allProducts.FirstOrDefault(item => item.Id == _productId);

        if (product is null
            || !string.Equals(CodeSearchText.Trim(), product.ProductCode, StringComparison.CurrentCultureIgnoreCase)
            || !string.Equals(NameSearchText.Trim(), product.NameAr, StringComparison.CurrentCultureIgnoreCase))
        {
            _productId = null;
            HasSelectedProduct = false;
            RegisteredLatestPriceText = "—";
        }
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
