using AutoPartsPOS.Application.Common.ViewModels;
using AutoPartsPOS.Application.Sales.Dtos;
using AutoPartsPOS.WPF.Sales.Services;
using CommunityToolkit.Mvvm.Input;

namespace AutoPartsPOS.WPF.Sales.ViewModels;

public sealed partial class SalesInvoiceDetailsViewModel(ISalesInvoicePrintService printService) : ViewModelBase
{
    public event EventHandler<bool?>? RequestClose;

    public SalesInvoiceDetailsDto Invoice { get; private set; } = new(
        0,
        string.Empty,
        DateOnly.FromDateTime(DateTime.Today),
        string.Empty,
        0,
        0,
        0,
        null,
        []);

    public void Load(SalesInvoiceDetailsDto invoice)
    {
        Invoice = invoice;
        Title = $"فاتورة بيع {invoice.InvoiceNumber}";
        OnPropertyChanged(nameof(Invoice));
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
}
