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
    private DateTime? _fromDate;

    [ObservableProperty]
    private DateTime? _toDate;

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
        if (FromDate is not null && ToDate is not null && FromDate.Value.Date > ToDate.Value.Date)
        {
            ErrorMessage = "تاريخ البداية يجب ألا يكون بعد تاريخ النهاية.";
            return;
        }

        await LoadAsync();
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        SearchText = null;
        FromDate = null;
        ToDate = null;
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
            if (await dialogService.ShowDetailsDialogAsync(details))
            {
                await LoadAsync();
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanVoidSelectedInvoice))]
    private async Task VoidAsync()
    {
        if (SelectedInvoice is null)
        {
            return;
        }

        if (!await dialogService.ShowCancelConfirmationAsync())
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
                DateOnly? fromDate = FromDate is null ? null : DateOnly.FromDateTime(FromDate.Value.Date);
                DateOnly? toDate = ToDate is null ? null : DateOnly.FromDateTime(ToDate.Value.Date);

                foreach (var invoice in await salesInvoiceService.SearchAsync(SearchText, fromDate, toDate, token))
                {
                    Invoices.Add(invoice);
                }

                ErrorMessage = null;
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
        return SelectedInvoice is not null && SelectedInvoice.Status != "ملغاة";
    }
}
