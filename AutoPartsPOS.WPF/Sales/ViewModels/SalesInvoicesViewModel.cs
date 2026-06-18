using AutoPartsPOS.Application.Common.ViewModels;
using AutoPartsPOS.Application.Sales.Dtos;
using AutoPartsPOS.Application.Sales.Interfaces;
using AutoPartsPOS.WPF.Sales.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AutoPartsPOS.WPF.Sales.ViewModels;

public sealed partial class SalesInvoicesViewModel(
    ISalesInvoiceService salesInvoiceService,
    ISalesDialogService dialogService) : ViewModelBase
{
    public ObservableCollection<SalesInvoiceListDto> Invoices { get; } = [];

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ViewCommand))]
    [NotifyCanExecuteChangedFor(nameof(VoidCommand))]
    private SalesInvoiceListDto? _selectedInvoice;

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Title = "فواتير البيع";
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

        var details = await salesInvoiceService.GetDetailsAsync(SelectedInvoice.Id);

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
            var result = await salesInvoiceService.VoidAsync(SelectedInvoice.Id, "إلغاء من شاشة فواتير البيع", cancellationToken);

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

                foreach (var invoice in await salesInvoiceService.SearchAsync(SearchText, token))
                {
                    Invoices.Add(invoice);
                }
            }, cancellationToken);
        }
        catch (Exception exception)
        {
            ErrorMessage = $"تعذر تحميل فواتير البيع: {exception.Message}";
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
