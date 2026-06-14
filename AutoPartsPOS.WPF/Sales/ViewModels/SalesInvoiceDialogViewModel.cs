using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.Application.Catalog.Interfaces;
using AutoPartsPOS.Application.Sales.Dtos;
using AutoPartsPOS.Application.Sales.Interfaces;
using AutoPartsPOS.WPF.Catalog.ViewModels;
using AutoPartsPOS.WPF.Sales.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AutoPartsPOS.WPF.Sales.ViewModels;

public sealed partial class SalesInvoiceDialogViewModel(
    ISalesInvoiceService salesInvoiceService,
    IProductService productService,
    ISalesInvoicePrintService printService) : ValidatableDialogViewModel
{
    private long? _lastSavedInvoiceId;

    public ObservableCollection<ProductDto> Products { get; } = [];

    public ObservableCollection<SalesInvoiceLineViewModel> Lines { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NetTotal))]
    private string _invoiceNumber = string.Empty;

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
    [NotifyPropertyChangedFor(nameof(NetTotal))]
    private decimal _discountAmount;

    [ObservableProperty]
    private SalesInvoiceLineViewModel? _selectedLine;

    public decimal Subtotal => Lines.Sum(line => line.TotalPrice);

    public decimal NetTotal => Subtotal - DiscountAmount;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        Title = "إنشاء فاتورة بيع";
        InvoiceNumber = $"SAL-{DateTime.Now:yyyyMMdd-HHmmss}";
        InvoiceDate = DateTime.Today;
        Notes = null;
        DiscountAmount = 0;
        Lines.Clear();
        Products.Clear();
        _lastSavedInvoiceId = null;

        foreach (var product in (await productService.SearchAsync(null, null, cancellationToken)).Where(product => product.IsActive))
        {
            Products.Add(product);
        }

        SelectedProduct = Products.FirstOrDefault();
        ApplySelectedProductPrice();
        ApplyErrors(new Dictionary<string, string[]>());
        NotifyTotalsChanged();
    }

    partial void OnSelectedProductChanged(ProductDto? value)
    {
        ApplySelectedProductPrice();
    }

    partial void OnDiscountAmountChanged(decimal value)
    {
        OnPropertyChanged(nameof(NetTotal));
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

        var existingQuantity = Lines
            .Where(line => line.Product?.Id == SelectedProduct.Id)
            .Sum(line => line.Quantity);

        if (SelectedProduct.CurrentStock < existingQuantity + Quantity)
        {
            ErrorMessage = $"لا يمكن بيع كمية أكبر من المتاح. المتاح: {SelectedProduct.CurrentStock:N3}.";
            return;
        }

        var existingLine = Lines.FirstOrDefault(line => line.Product?.Id == SelectedProduct.Id && line.UnitPrice == UnitPrice);

        if (existingLine is null)
        {
            var line = new SalesInvoiceLineViewModel
            {
                Product = SelectedProduct,
                Quantity = Quantity,
                UnitPrice = UnitPrice
            };

            line.PropertyChanged += (_, _) => NotifyTotalsChanged();
            Lines.Add(line);
        }
        else
        {
            existingLine.Quantity += Quantity;
        }

        ErrorMessage = null;
        Quantity = 1;
        NotifyTotalsChanged();
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
        NotifyTotalsChanged();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await SaveInternalAsync(false);
    }

    [RelayCommand]
    private async Task SaveAndPrintAsync()
    {
        await SaveInternalAsync(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        Close(false);
    }

    private async Task SaveInternalAsync(bool printAfterSave)
    {
        await ExecuteBusyAsync(async cancellationToken =>
        {
            var result = await salesInvoiceService.CreateAsync(CreateDto(), cancellationToken);

            if (!result.Succeeded)
            {
                ApplyErrors(result.Errors);
                ErrorMessage = result.ErrorSummary;
                return;
            }

            var savedInvoice = (await salesInvoiceService.SearchAsync(InvoiceNumber, cancellationToken))
                .FirstOrDefault(invoice => invoice.InvoiceNumber == InvoiceNumber);
            _lastSavedInvoiceId = savedInvoice?.Id;

            if (printAfterSave && _lastSavedInvoiceId is not null)
            {
                var details = await salesInvoiceService.GetDetailsAsync(_lastSavedInvoiceId.Value, cancellationToken);

                if (details is not null)
                {
                    await printService.PrintAsync(details, cancellationToken);
                }
            }

            Close(true);
        });
    }

    private CreateSalesInvoiceDto CreateDto()
    {
        return new CreateSalesInvoiceDto
        {
            InvoiceNumber = InvoiceNumber,
            InvoiceDate = DateOnly.FromDateTime(InvoiceDate ?? DateTime.Today),
            Notes = Notes,
            DiscountAmount = DiscountAmount,
            Items = Lines
                .Where(line => line.Product is not null)
                .Select(line => new CreateSalesInvoiceItemDto
                {
                    ProductId = line.Product!.Id,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice
                })
                .ToList()
        };
    }

    private void ApplySelectedProductPrice()
    {
        if (SelectedProduct is not null)
        {
            UnitPrice = SelectedProduct.SellingPrice;
        }
    }

    private void NotifyTotalsChanged()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(NetTotal));
    }
}
