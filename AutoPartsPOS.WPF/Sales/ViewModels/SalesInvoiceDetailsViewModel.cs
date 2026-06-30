using AutoPartsPOS.Application.Common.ViewModels;
using AutoPartsPOS.Application.Sales.Dtos;
using AutoPartsPOS.WPF.Helpers;
using AutoPartsPOS.WPF.Sales.Services;
using AutoPartsPOS.Application.Sales.Interfaces;
using AutoPartsPOS.Domain.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AutoPartsPOS.WPF.Sales.ViewModels;

public sealed partial class SalesInvoiceDetailsViewModel(
    ISalesInvoicePrintService printService,
    ISalesInvoiceService salesInvoiceService) : ViewModelBase
{
    private InvoicePaymentStatus _loadedPaymentStatus;
    private decimal _loadedPaidAmount;

    public event EventHandler<bool?>? RequestClose;

    public SalesInvoiceDetailsDto Invoice { get; private set; } = new(
        0,
        string.Empty,
        DateOnly.FromDateTime(DateTime.Today),
        string.Empty,
        InvoicePaymentStatus.Unpaid,
        false,
        0,
        0,
        0,
        0,
        0,
        null,
        []);

    public ObservableCollection<KeyValuePair<InvoicePaymentStatus, string>> PaymentStatuses { get; } =
    [
        new(InvoicePaymentStatus.Paid, "مدفوعة بالكامل"),
        new(InvoicePaymentStatus.PartiallyPaid, "مدفوعة جزئياً"),
        new(InvoicePaymentStatus.Unpaid, "غير مدفوعة")
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPartiallyPaid))]
    [NotifyPropertyChangedFor(nameof(HasPendingPaymentChanges))]
    private InvoicePaymentStatus _selectedPaymentStatus;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPendingPaymentChanges))]
    private string? _paidAmountText;

    [ObservableProperty]
    private decimal _remainingAmount;

    public bool IsPartiallyPaid => SelectedPaymentStatus == InvoicePaymentStatus.PartiallyPaid;

    public bool CanEditPayment => !Invoice.IsVoided;

    public bool HasPendingPaymentChanges =>
        CanEditPayment &&
        (SelectedPaymentStatus != _loadedPaymentStatus ||
         GetEffectivePaidAmount() != _loadedPaidAmount);

    public void Load(SalesInvoiceDetailsDto invoice)
    {
        Invoice = invoice;
        _loadedPaymentStatus = invoice.PaymentStatus;
        _loadedPaidAmount = invoice.PaidAmount;
        SelectedPaymentStatus = invoice.PaymentStatus;
        PaidAmountText = invoice.PaymentStatus == InvoicePaymentStatus.PartiallyPaid && invoice.PaidAmount > 0
            ? WholeNumberInput.Format(invoice.PaidAmount)
            : null;
        RemainingAmount = invoice.RemainingAmount;
        Title = "تفاصيل فاتورة البيع";
        OnPropertyChanged(nameof(Invoice));
        OnPropertyChanged(nameof(CanEditPayment));
        OnPropertyChanged(nameof(HasPendingPaymentChanges));
    }

    partial void OnSelectedPaymentStatusChanged(InvoicePaymentStatus value)
    {
        SyncPaymentAmounts();
        OnPropertyChanged(nameof(HasPendingPaymentChanges));
    }

    partial void OnPaidAmountTextChanged(string? value)
    {
        if (SelectedPaymentStatus == InvoicePaymentStatus.PartiallyPaid &&
            WholeNumberInput.TryParseOptional(PaidAmountText, out var paidAmount))
        {
            RemainingAmount = Math.Max(Invoice.NetTotalAmount - paidAmount, 0);
        }

        OnPropertyChanged(nameof(HasPendingPaymentChanges));
    }

    [RelayCommand]
    private async Task SavePaymentAsync()
    {
        if (SelectedPaymentStatus == InvoicePaymentStatus.PartiallyPaid)
        {
            if (!WholeNumberInput.TryParseRequired(
                    PaidAmountText,
                    out var paidAmount,
                    out var paidError,
                    "المبلغ المدفوع مطلوب",
                    "المبلغ المدفوع يجب أن يكون عدداً صحيحاً"))
            {
                ErrorMessage = paidError;
                return;
            }

            if (paidAmount <= 0)
            {
                ErrorMessage = "المبلغ المدفوع يجب أن يكون أكبر من صفر.";
                return;
            }

            if (paidAmount >= Invoice.NetTotalAmount)
            {
                ErrorMessage = "المبلغ المدفوع لا يمكن أن يتجاوز إجمالي الفاتورة.";
                return;
            }

            var result = await salesInvoiceService.UpdatePaymentAsync(Invoice.Id, SelectedPaymentStatus, paidAmount);

            if (!result.Succeeded)
            {
                ErrorMessage = result.ErrorSummary;
                return;
            }

            RequestClose?.Invoke(this, true);
            return;
        }

        var updateResult = await salesInvoiceService.UpdatePaymentAsync(Invoice.Id, SelectedPaymentStatus, 0);

        if (!updateResult.Succeeded)
        {
            ErrorMessage = updateResult.ErrorSummary;
            return;
        }

        RequestClose?.Invoke(this, true);
    }

    [RelayCommand]
    private async Task PreviewAsync()
    {
        await printService.PreviewAsync(Invoice);
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        await printService.PrintAsync(Invoice);
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke(this, true);
    }

    private void SyncPaymentAmounts()
    {
        switch (SelectedPaymentStatus)
        {
            case InvoicePaymentStatus.Paid:
                PaidAmountText = Invoice.NetTotalAmount > 0
                    ? WholeNumberInput.Format(Invoice.NetTotalAmount)
                    : null;
                RemainingAmount = 0;
                break;
            case InvoicePaymentStatus.Unpaid:
                PaidAmountText = null;
                RemainingAmount = Invoice.NetTotalAmount;
                break;
            case InvoicePaymentStatus.PartiallyPaid:
                if (WholeNumberInput.TryParseOptional(PaidAmountText, out var paidAmount) && paidAmount >= Invoice.NetTotalAmount)
                {
                    PaidAmountText = null;
                    paidAmount = 0;
                }

                RemainingAmount = Math.Max(Invoice.NetTotalAmount - paidAmount, 0);
                break;
        }
    }

    private decimal GetEffectivePaidAmount()
    {
        return SelectedPaymentStatus switch
        {
            InvoicePaymentStatus.Paid => Invoice.NetTotalAmount,
            InvoicePaymentStatus.Unpaid => 0,
            InvoicePaymentStatus.PartiallyPaid when WholeNumberInput.TryParseOptional(PaidAmountText, out var paidAmount) => paidAmount,
            _ => 0
        };
    }
}
