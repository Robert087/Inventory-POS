using AutoPartsPOS.Application.Common.ViewModels;
using AutoPartsPOS.Application.Purchases.Dtos;
using AutoPartsPOS.Application.Purchases.Interfaces;
using AutoPartsPOS.WPF.Purchases.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AutoPartsPOS.WPF.Purchases.ViewModels;

public sealed partial class PurchaseInvoicesViewModel(
    IPurchaseInvoiceService purchaseInvoiceService,
    IPurchaseDialogService dialogService) : ViewModelBase
{
    public ObservableCollection<PurchaseInvoiceListDto> Invoices { get; } = [];

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ViewCommand))]
    [NotifyCanExecuteChangedFor(nameof(VoidCommand))]
    private PurchaseInvoiceListDto? _selectedInvoice;

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Title = "فواتير الشراء";
        await LoadAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (await dialogService.ShowCreateDialogAsync())
        {
            await LoadAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedInvoice))]
    private async Task ViewAsync()
    {
        if (SelectedInvoice is null)
        {
            return;
        }

        await ViewInvoiceAsync(SelectedInvoice);
    }

    [RelayCommand]
    private async Task ViewRowAsync(PurchaseInvoiceListDto? invoice)
    {
        if (invoice is null)
        {
            return;
        }

        await ViewInvoiceAsync(invoice);
    }

    private async Task ViewInvoiceAsync(PurchaseInvoiceListDto invoice)
    {
        var details = await purchaseInvoiceService.GetDetailsAsync(invoice.Id);

        if (details is not null)
        {
            await dialogService.ShowDetailsDialogAsync(details);
        }
    }

    [RelayCommand(CanExecute = nameof(CanVoidSelectedInvoice))]
    private async Task VoidAsync()
    {
        if (SelectedInvoice is null)
        {
            return;
        }

        await ExecuteBusyAsync(async cancellationToken =>
        {
            var result = await purchaseInvoiceService.VoidAsync(SelectedInvoice.Id, "إلغاء من شاشة فواتير الشراء", cancellationToken);

            if (!result.Succeeded)
            {
                ErrorMessage = result.ErrorSummary;
                return;
            }

            await LoadAsync(cancellationToken);
        });
    }

    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await ExecuteBusyAsync(async token =>
            {
                Invoices.Clear();

                foreach (var invoice in await purchaseInvoiceService.SearchAsync(SearchText, token))
                {
                    Invoices.Add(invoice);
                }
            }, cancellationToken);
        }
        catch (Exception exception)
        {
            ErrorMessage = $"تعذر تحميل فواتير الشراء: {exception.Message}";
        }
    }

    private bool HasSelectedInvoice()
    {
        return SelectedInvoice is not null;
    }

    private bool CanVoidSelectedInvoice()
    {
        return SelectedInvoice?.Status == "مُرحّلة";
    }
}
