using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.Application.Catalog.Interfaces;
using AutoPartsPOS.Application.Purchases.Dtos;
using AutoPartsPOS.Application.Purchases.Interfaces;
using AutoPartsPOS.Application.Suppliers.Dtos;
using AutoPartsPOS.Application.Suppliers.Interfaces;
using AutoPartsPOS.WPF.Catalog.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AutoPartsPOS.WPF.Purchases.ViewModels;

public sealed partial class PurchaseInvoiceDialogViewModel(
    IPurchaseInvoiceService purchaseInvoiceService,
    ISupplierService supplierService,
    IProductService productService) : ValidatableDialogViewModel
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
    private decimal _quantity = 1;

    [ObservableProperty]
    private decimal _unitPrice;

    [ObservableProperty]
    private PurchaseInvoiceLineViewModel? _selectedLine;

    public decimal InvoiceTotal => Lines.Sum(line => line.TotalPrice);

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        Title = "إنشاء فاتورة شراء";
        InvoiceNumber = $"PUR-{DateTime.Now:yyyyMMdd-HHmmss}";
        InvoiceDate = DateTime.Today;
        Notes = null;
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

        if (Quantity <= 0)
        {
            ErrorMessage = "الكمية يجب أن تكون أكبر من صفر.";
            return;
        }

        if (UnitPrice < 0)
        {
            ErrorMessage = "سعر الوحدة لا يمكن أن يكون أقل من صفر.";
            return;
        }

        var existingLine = Lines.FirstOrDefault(line => line.Product?.Id == SelectedProduct.Id && line.UnitPrice == UnitPrice);

        if (existingLine is null)
        {
            var line = new PurchaseInvoiceLineViewModel
            {
                Product = SelectedProduct,
                Quantity = Quantity,
                UnitPrice = UnitPrice
            };

            line.PropertyChanged += (_, _) => OnPropertyChanged(nameof(InvoiceTotal));
            Lines.Add(line);
        }
        else
        {
            existingLine.Quantity += Quantity;
        }

        ErrorMessage = null;
        Quantity = 1;
        OnPropertyChanged(nameof(InvoiceTotal));
    }

    [RelayCommand]
    private void RemoveLine()
    {
        if (SelectedLine is null)
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
        if (SelectedProduct is not null)
        {
            UnitPrice = SelectedProduct.PurchasePrice;
        }
    }
}
