using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.Application.Catalog.Interfaces;
using AutoPartsPOS.Application.Sales.Dtos;
using AutoPartsPOS.Application.Sales.Interfaces;
using AutoPartsPOS.WPF.Catalog.ViewModels;
using AutoPartsPOS.WPF.Helpers;
using AutoPartsPOS.WPF.Sales.Services;
using AutoPartsPOS.Domain.Common;
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

    public ObservableCollection<KeyValuePair<InvoicePaymentStatus, string>> PaymentStatuses { get; } =
    [
        new(InvoicePaymentStatus.Paid, "مدفوعة بالكامل"),
        new(InvoicePaymentStatus.PartiallyPaid, "مدفوعة جزئياً"),
        new(InvoicePaymentStatus.Unpaid, "غير مدفوعة")
    ];

    [ObservableProperty]
    private string _invoiceNumber = string.Empty;

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
    [NotifyPropertyChangedFor(nameof(NetTotal))]
    private string? _discountAmountText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPartiallyPaid))]
    private InvoicePaymentStatus? _selectedPaymentStatus;

    [ObservableProperty]
    private string? _paidAmountText;

    [ObservableProperty]
    private decimal _remainingAmount;

    [ObservableProperty]
    private SalesInvoiceLineViewModel? _selectedLine;

    public decimal DiscountDisplay => ParseOptionalAmount(DiscountAmountText);

    public decimal Subtotal => Lines.Sum(line => line.TotalPrice);

    public decimal NetTotal => Subtotal - ParseOptionalAmount(DiscountAmountText);

    public bool IsPartiallyPaid => SelectedPaymentStatus == InvoicePaymentStatus.PartiallyPaid;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        Title = "إنشاء فاتورة بيع";
        InvoiceNumber = $"SAL-{DateTime.Now:yyyyMMdd-HHmmss}";
        InvoiceDate = DateTime.Today;
        Notes = null;
        DiscountAmountText = null;
        SelectedPaymentStatus = null;
        PaidAmountText = null;
        QuantityText = null;
        UnitPriceText = null;
        RemainingAmount = 0;
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

    partial void OnDiscountAmountTextChanged(string? value)
    {
        OnPropertyChanged(nameof(DiscountDisplay));
        NotifyTotalsChanged();
    }

    partial void OnSelectedPaymentStatusChanged(InvoicePaymentStatus? value)
    {
        SyncPaymentAmounts();
    }

    partial void OnPaidAmountTextChanged(string? value)
    {
        if (SelectedPaymentStatus == InvoicePaymentStatus.PartiallyPaid &&
            WholeNumberInput.TryParseOptional(PaidAmountText, out var paidAmount))
        {
            RemainingAmount = Math.Max(NetTotal - paidAmount, 0);
        }
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
                "سعر البيع مطلوب",
                "سعر البيع يجب أن يكون أكبر من صفر",
                "سعر البيع يجب أن يكون عدداً صحيحاً"))
        {
            ErrorMessage = unitPriceError;
            return;
        }

        var existingQuantity = Lines
            .Where(line => line.Product?.Id == SelectedProduct.Id)
            .Sum(line => line.Quantity);

        if (SelectedProduct.CurrentStock < existingQuantity + quantity)
        {
            ErrorMessage = $"لا يمكن بيع كمية أكبر من المتاح. المتاح: {WholeNumberInput.Format(SelectedProduct.CurrentStock)}.";
            return;
        }

        var existingLine = Lines.FirstOrDefault(line => line.Product?.Id == SelectedProduct.Id && line.UnitPrice == unitPrice);

        if (existingLine is null)
        {
            var line = new SalesInvoiceLineViewModel
            {
                Product = SelectedProduct,
                Quantity = quantity,
                UnitPrice = unitPrice
            };

            line.PropertyChanged += (_, _) => NotifyTotalsChanged();
            Lines.Add(line);
        }
        else
        {
            existingLine.Quantity += quantity;
        }

        ErrorMessage = null;
        QuantityText = null;
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
        if (!TryBuildDto(out var dto, out var validationErrors))
        {
            ApplyErrors(validationErrors);
            ErrorMessage = validationErrors.Values.SelectMany(messages => messages).FirstOrDefault();
            return;
        }

        await ExecuteBusyAsync(async cancellationToken =>
        {
            var result = await salesInvoiceService.CreateAsync(dto, cancellationToken);

            if (!result.Succeeded)
            {
                ApplyErrors(result.Errors);
                ErrorMessage = result.ErrorSummary;
                return;
            }

            var savedInvoice = (await salesInvoiceService.SearchAsync(InvoiceNumber, null, null, cancellationToken))
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

    private bool TryBuildDto(out CreateSalesInvoiceDto dto, out Dictionary<string, string[]> errors)
    {
        errors = new Dictionary<string, string[]>();
        dto = new CreateSalesInvoiceDto();

        if (!WholeNumberInput.TryParseOptional(DiscountAmountText, out var discountAmount))
        {
            AddValidationError(errors, nameof(DiscountAmountText), "الخصم يجب أن يكون عدداً صحيحاً.");
        }

        if (SelectedPaymentStatus is null)
        {
            AddValidationError(errors, nameof(SelectedPaymentStatus), "يجب اختيار حالة الدفع.");
        }

        var paidAmount = 0m;
        if (SelectedPaymentStatus == InvoicePaymentStatus.PartiallyPaid)
        {
            if (!WholeNumberInput.TryParseRequired(
                    PaidAmountText,
                    out paidAmount,
                    out var paidError,
                    "المبلغ المدفوع مطلوب",
                    "المبلغ المدفوع يجب أن يكون عدداً صحيحاً"))
            {
                AddValidationError(errors, nameof(PaidAmountText), paidError!);
            }
            else if (paidAmount <= 0)
            {
                AddValidationError(errors, nameof(PaidAmountText), "المبلغ المدفوع يجب أن يكون أكبر من صفر.");
            }
            else if (paidAmount >= NetTotal)
            {
                AddValidationError(errors, nameof(PaidAmountText), "المبلغ المدفوع لا يمكن أن يتجاوز إجمالي الفاتورة.");
            }
        }
        else if (!WholeNumberInput.TryParseOptional(PaidAmountText, out paidAmount))
        {
            AddValidationError(errors, nameof(PaidAmountText), "المبلغ المدفوع يجب أن يكون عدداً صحيحاً.");
        }

        if (Lines.Count == 0)
        {
            AddValidationError(errors, nameof(Lines), "يجب إضافة منتج واحد على الأقل.");
        }

        if (errors.Count > 0)
        {
            return false;
        }

        dto = new CreateSalesInvoiceDto
        {
            InvoiceNumber = InvoiceNumber,
            InvoiceDate = DateOnly.FromDateTime(InvoiceDate ?? DateTime.Today),
            Notes = Notes,
            DiscountAmount = discountAmount,
            PaymentStatus = SelectedPaymentStatus,
            PaidAmount = paidAmount,
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

        return true;
    }

    private static void AddValidationError(Dictionary<string, string[]> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var messages))
        {
            messages = [];
            errors[key] = messages;
        }

        errors[key] = [.. messages, message];
    }

    private void ApplySelectedProductPrice()
    {
        if (SelectedProduct is null)
        {
            UnitPriceText = null;
            return;
        }

        UnitPriceText = SelectedProduct.SellingPrice > 0
            ? WholeNumberInput.Format(SelectedProduct.SellingPrice)
            : null;
    }

    private void NotifyTotalsChanged()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(NetTotal));
        SyncPaymentAmounts();
    }

    private void SyncPaymentAmounts()
    {
        switch (SelectedPaymentStatus)
        {
            case InvoicePaymentStatus.Paid:
                PaidAmountText = NetTotal > 0 ? WholeNumberInput.Format(NetTotal) : null;
                RemainingAmount = 0;
                break;
            case InvoicePaymentStatus.Unpaid:
                PaidAmountText = null;
                RemainingAmount = NetTotal;
                break;
            case InvoicePaymentStatus.PartiallyPaid:
                if (WholeNumberInput.TryParseOptional(PaidAmountText, out var paidAmount) && paidAmount >= NetTotal)
                {
                    PaidAmountText = null;
                    paidAmount = 0;
                }

                RemainingAmount = Math.Max(NetTotal - paidAmount, 0);
                break;
            default:
                PaidAmountText = null;
                RemainingAmount = NetTotal;
                break;
        }
    }

    private static decimal ParseOptionalAmount(string? text) =>
        WholeNumberInput.TryParseOptional(text, out var value) ? value : 0;
}
