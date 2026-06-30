using AutoPartsPOS.Application.HomeExpenses.Dtos;
using AutoPartsPOS.Application.HomeExpenses.Interfaces;
using AutoPartsPOS.WPF.Catalog.ViewModels;
using AutoPartsPOS.WPF.HomeExpenses.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AutoPartsPOS.WPF.HomeExpenses.ViewModels;

public sealed partial class HomeExpenseDetailsDialogViewModel(
    IHomeExpenseService homeExpenseService,
    IHomeExpenseDialogService dialogService) : ValidatableDialogViewModel
{
    private long _dayId;
    private readonly List<long> _pendingDeletedItemIds = [];
    private List<HomeExpenseItemDto> _loadedItemsSnapshot = [];

    [ObservableProperty]
    private DateOnly _expenseDate;

    [ObservableProperty]
    private decimal _totalAmount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _hasPendingChanges;

    public ObservableCollection<HomeExpenseItemDto> Items { get; } = [];

    public async Task LoadAsync(long dayId, CancellationToken cancellationToken = default)
    {
        _dayId = dayId;
        _pendingDeletedItemIds.Clear();
        HasPendingChanges = false;
        Items.Clear();
        _loadedItemsSnapshot = [];
        ErrorMessage = null;

        var details = await homeExpenseService.GetDetailsAsync(dayId, cancellationToken);

        if (details is null)
        {
            ErrorMessage = "تعذر تحميل تفاصيل المصاريف.";
            return;
        }

        ExpenseDate = details.ExpenseDate;
        TotalAmount = details.TotalAmount;
        _loadedItemsSnapshot = [.. details.Items];

        foreach (var item in details.Items)
        {
            Items.Add(item);
        }
    }

    [RelayCommand]
    private void DeleteItem(HomeExpenseItemDto? item)
    {
        if (item is null || !dialogService.ShowDeleteConfirmationDialog())
        {
            return;
        }

        Items.Remove(item);
        _pendingDeletedItemIds.Add(item.Id);
        RecalculateTotal();
        HasPendingChanges = true;
        ErrorMessage = null;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (_pendingDeletedItemIds.Count == 0)
        {
            return;
        }

        await ExecuteBusyAsync(async cancellationToken =>
        {
            foreach (var itemId in _pendingDeletedItemIds.ToArray())
            {
                var result = await homeExpenseService.DeleteItemAsync(itemId, cancellationToken);

                if (!result.Succeeded)
                {
                    ErrorMessage = result.ErrorSummary;
                    return;
                }
            }

            _pendingDeletedItemIds.Clear();
            HasPendingChanges = false;

            if (Items.Count == 0)
            {
                Close(true);
                return;
            }

            await LoadAsync(_dayId, cancellationToken);
            Close(true);
        });
    }

    [RelayCommand]
    private async Task AddItemAsync()
    {
        if (HasPendingChanges)
        {
            ErrorMessage = "يرجى حفظ التغييرات قبل إضافة مصروف جديد.";
            return;
        }

        if (await dialogService.ShowAddDialogAsync(ExpenseDate))
        {
            await LoadAsync(_dayId);
            Close(true);
        }
    }

    [RelayCommand]
    private void CloseDialog()
    {
        DiscardPendingChanges();
        Close(false);
    }

    public void DiscardPendingChanges()
    {
        if (!HasPendingChanges)
        {
            return;
        }

        _pendingDeletedItemIds.Clear();
        Items.Clear();

        foreach (var item in _loadedItemsSnapshot)
        {
            Items.Add(item);
        }

        TotalAmount = _loadedItemsSnapshot.Sum(item => item.Amount);
        HasPendingChanges = false;
    }

    private void RecalculateTotal()
    {
        TotalAmount = Items.Sum(item => item.Amount);
    }

    private bool CanSave()
    {
        return HasPendingChanges;
    }
}
