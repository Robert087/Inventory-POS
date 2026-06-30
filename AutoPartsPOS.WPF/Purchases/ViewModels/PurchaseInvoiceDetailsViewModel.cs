using AutoPartsPOS.Application.Purchases.Dtos;
using AutoPartsPOS.Application.Common.ViewModels;
using CommunityToolkit.Mvvm.Input;

namespace AutoPartsPOS.WPF.Purchases.ViewModels;

public sealed partial class PurchaseInvoiceDetailsViewModel : ViewModelBase
{
    public event EventHandler<bool?>? RequestClose;

    public PurchaseInvoiceDetailsDto Invoice { get; private set; } = new(
        0,
        string.Empty,
        0,
        string.Empty,
        DateOnly.FromDateTime(DateTime.Today),
        string.Empty,
        0,
        null,
        []);

    public void Load(PurchaseInvoiceDetailsDto invoice)
    {
        Invoice = invoice;
        Title = "تفاصيل فاتورة الشراء";
        OnPropertyChanged(nameof(Invoice));
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke(this, true);
    }
}
