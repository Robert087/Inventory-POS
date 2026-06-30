using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.Application.Catalog.Interfaces;
using AutoPartsPOS.Application.Purchases.Dtos;
using AutoPartsPOS.Application.Purchases.Interfaces;
using AutoPartsPOS.Application.Suppliers.Dtos;
using AutoPartsPOS.Application.Suppliers.Interfaces;
using AutoPartsPOS.WPF.Catalog.ViewModels;
using AutoPartsPOS.WPF.Helpers;
using AutoPartsPOS.WPF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AutoPartsPOS.WPF.Purchases.ViewModels;

public sealed partial class PurchaseInvoiceDialogViewModel(
    IPurchaseInvoiceService purchaseInvoiceService,
    ISupplierService supplierService,
    IProductService productService,
    IDeleteConfirmationService deleteConfirmationService) : ValidatableDialogViewModel
{
    public ObservableCollection<SupplierDto> Suppliers { get; } = [];

    public ObservableCollection<ProductDto> Products { get; } = [];

    public ObservableCollection<PurchaseInvoiceLineViewModel> Lines { get; } = [];

    [ObservableProperty]
    private string _invoiceNumber = string.Empty;

    [ObservableProperty]
    private SupplierDto? _selectedSupplier;

    [ObservableProperty]
    private DateTime? _invoiceDate = DateTime.Today;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private ProductDto? _selectedProduct;

    [ObservableProperty]
    private string? _quantityText;

    [ObservableProperty]
    private string? _unitPriceText;

    [ObservableProperty]
    private PurchaseInvoiceLineViewModel? _selectedLine;

    public decimal InvoiceTotal => Lines.Sum(line => line.TotalPrice);

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        Title = "إنشاء فاتورة شراء";
        InvoiceNumber = $"PUR-{DateTime.Now:yyyyMMdd-HHmmss}";
        InvoiceDate = DateTime.Today;
        Notes = null;
        QuantityText = null;
        UnitPriceText = null;
        Lines.Clear();
        Suppliers.Clear();
        Products.Clear();

        foreach (var supplier in (await supplierService.SearchAsync(null, cancellationToken)).Where(supplier => supplier.IsActive))
        {
            Suppliers.Add(supplier);
        }

        foreach (var product in (await productService.SearchAsync(null, null, cancellationToken)).Where(product => product.IsActive))
        {
            Products.Add(product);
        }

        SelectedSupplier = Suppliers.FirstOrDefault();
        SelectedProduct = Products.FirstOrDefault();
        ApplySelectedProductPrice();
        ApplyErrors(new Dictionary<string, string[]>());
        OnPropertyChanged(nameof(InvoiceTotal));
    }

    partial void OnSelectedProductChanged(ProductDto? value)
    {
        ApplySelectedProductPrice();
    }

    [RelayCommand]
    private void AddLine()
    {
        if (SelectedProduct is null)
        {
            ErrorMessage = "يجب اختيار منتج.";
            return;
        }

        if (!WholeNumberInput.TryParseRequiredPositive(
                QuantityText,
                out var quantity,
                out var quantityError,
                "الكمية مطلوبة",
                "الكمية يجب أن تكون أكبر من صفر",
                "الكمية يجب أن تكون عدداً صحيحاً"))
        {
            ErrorMessage = quantityError;
            return;
        }

        if (!WholeNumberInput.TryParseRequiredPositive(
                UnitPriceText,
                out var unitPrice,
                out var unitPriceError,
                "سعر الوحدة مطلوب",
                "سعر الوحدة يجب أن يكون أكبر من صفر",
                "سعر الوحدة يجب أن يكون عدداً صحيحاً"))
        {
            ErrorMessage = unitPriceError;
            return;
        }

        var existingLine = Lines.FirstOrDefault(line => line.Product?.Id == SelectedProduct.Id && line.UnitPrice == unitPrice);

        if (existingLine is null)
        {
            var line = new PurchaseInvoiceLineViewModel
            {
                Product = SelectedProduct,
                Quantity = quantity,
                UnitPrice = unitPrice
            };

            line.PropertyChanged += (_, _) => OnPropertyChanged(nameof(InvoiceTotal));
            Lines.Add(line);
        }
        else
        {
            existingLine.Quantity += quantity;
        }

        ErrorMessage = null;
        QuantityText = null;
        OnPropertyChanged(nameof(InvoiceTotal));
    }

    [RelayCommand]
    private void RemoveLine()
    {
        if (SelectedLine is null ||
            !deleteConfirmationService.ConfirmLineRemoval(SelectedLine.Product?.NameAr ?? "السطر المحدد"))
        {
            return;
        }

        Lines.Remove(SelectedLine);
        SelectedLine = null;
        OnPropertyChanged(nameof(InvoiceTotal));
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await ExecuteBusyAsync(async cancellationToken =>
        {
            var dto = new CreatePurchaseInvoiceDto
            {
                InvoiceNumber = InvoiceNumber,
                SupplierId = SelectedSupplier?.Id ?? 0,
                InvoiceDate = DateOnly.FromDateTime(InvoiceDate ?? DateTime.Today),
                Notes = Notes,
                Items = Lines
                    .Where(line => line.Product is not null)
                    .Select(line => new CreatePurchaseInvoiceItemDto
                    {
                        ProductId = line.Product!.Id,
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice
                    })
                    .ToList()
            };

            var result = await purchaseInvoiceService.CreateAsync(dto, cancellationToken);

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

    private void ApplySelectedProductPrice()
    {
        if (SelectedProduct is null)
        {
            UnitPriceText = null;
            return;
        }

        UnitPriceText = SelectedProduct.PurchasePrice > 0
            ? WholeNumberInput.Format(SelectedProduct.PurchasePrice)
            : null;
    }
}
