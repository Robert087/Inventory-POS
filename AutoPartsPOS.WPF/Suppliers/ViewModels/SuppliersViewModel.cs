using AutoPartsPOS.Application.Common.ViewModels;
using AutoPartsPOS.Application.Suppliers.Dtos;
using AutoPartsPOS.Application.Suppliers.Interfaces;
using AutoPartsPOS.WPF.Suppliers.Services;
using AutoPartsPOS.WPF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AutoPartsPOS.WPF.Suppliers.ViewModels;

public sealed partial class SuppliersViewModel(
    ISupplierService supplierService,
    ISupplierDialogService dialogService,
    IDeleteConfirmationService deleteConfirmationService) : ViewModelBase
{
    public ObservableCollection<SupplierDto> Suppliers { get; } = [];

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeactivateCommand))]
    private SupplierDto? _selectedSupplier;

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Title = "الموردون";
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
        if (await dialogService.ShowSupplierDialogAsync(null))
        {
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task EditRowAsync(SupplierDto? supplier)
    {
        if (supplier is not null && await dialogService.ShowSupplierDialogAsync(supplier))
        {
            await LoadAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedSupplier))]
    private async Task DeactivateAsync()
    {
        if (SelectedSupplier is null)
        {
            return;
        }

        await ExecuteBusyAsync(async cancellationToken =>
        {
            var result = await supplierService.DeactivateAsync(SelectedSupplier.Id, cancellationToken);

            if (!result.Succeeded)
            {
                ErrorMessage = result.ErrorSummary;
                return;
            }

            await LoadAsync(cancellationToken);
        });
    }

    [RelayCommand]
    private async Task DeleteRowAsync(SupplierDto? supplier)
    {
        if (supplier is null || !deleteConfirmationService.Confirm("المورد", supplier.NameAr))
        {
            return;
        }

        await ExecuteBusyAsync(async cancellationToken =>
        {
            var result = await supplierService.DeleteAsync(supplier.Id, cancellationToken);
            if (!result.Succeeded)
            {
                ErrorMessage = result.ErrorSummary;
                return;
            }

            Suppliers.Remove(supplier);
            if (SelectedSupplier?.Id == supplier.Id)
            {
                SelectedSupplier = null;
            }
        });
    }

    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await ExecuteBusyAsync(async token =>
        {
            Suppliers.Clear();

            foreach (var supplier in await supplierService.SearchAsync(SearchText, token))
            {
                Suppliers.Add(supplier);
            }
        }, cancellationToken);
    }

    private bool HasSelectedSupplier()
    {
        return SelectedSupplier is not null;
    }
}
