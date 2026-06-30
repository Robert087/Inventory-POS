using AutoPartsPOS.Application.Common.ViewModels;
using AutoPartsPOS.Application.LatestPrices.Dtos;
using AutoPartsPOS.Application.LatestPrices.Interfaces;
using AutoPartsPOS.WPF.LatestPrices.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AutoPartsPOS.WPF.LatestPrices.ViewModels;

public sealed partial class LatestPricesViewModel(
    ILatestPriceService latestPriceService,
    ILatestPriceDialogService dialogService) : ViewModelBase
{
    public ObservableCollection<LatestPriceDto> LatestPrices { get; } = [];

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    private LatestPriceDto? _selectedLatestPrice;

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Title = "أحدث الأسعار";
        await LoadAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (await dialogService.ShowLatestPriceDialogAsync(null))
        {
            await LoadAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedLatestPrice))]
    private async Task EditAsync()
    {
        if (SelectedLatestPrice is not null && await dialogService.ShowLatestPriceDialogAsync(SelectedLatestPrice))
        {
            await LoadAsync();
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await ExecuteBusyAsync(async token =>
        {
            LatestPrices.Clear();

            foreach (var latestPrice in await latestPriceService.SearchAsync(SearchText, token))
            {
                LatestPrices.Add(latestPrice);
            }
        }, cancellationToken);
    }

    private bool HasSelectedLatestPrice()
    {
        return SelectedLatestPrice is not null;
    }
}
