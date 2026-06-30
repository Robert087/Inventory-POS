using AutoPartsPOS.Application.HomeExpenses.Dtos;
using AutoPartsPOS.Application.HomeExpenses.Interfaces;
using AutoPartsPOS.WPF.Catalog.ViewModels;
using AutoPartsPOS.WPF.Helpers;
using AutoPartsPOS.WPF.HomeExpenses.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AutoPartsPOS.WPF.HomeExpenses.ViewModels;

public sealed partial class HomeExpenseDialogViewModel(IHomeExpenseService homeExpenseService) : ValidatableDialogViewModel
{
    [ObservableProperty]
    private DateTime? _expenseDate = DateTime.Today;

    [ObservableProperty]
    private bool _isDateEditable = true;

    [ObservableProperty]
    private string? _noteText;

    [ObservableProperty]
    private string? _amountText;

    [ObservableProperty]
    private string _currentTotalText = "0";

    public ObservableCollection<PendingHomeExpenseItem> PendingItems { get; } = [];

    public void Load(DateOnly? presetDate = null)
    {
        PendingItems.Clear();
        NoteText = null;
        AmountText = null;
        ApplyErrors(new Dictionary<string, string[]>());
        ErrorMessage = null;

        if (presetDate is not null)
        {
            ExpenseDate = presetDate.Value.ToDateTime(TimeOnly.MinValue);
            IsDateEditable = false;
        }
        else
        {
            ExpenseDate = DateTime.Today;
            IsDateEditable = true;
        }

        UpdateCurrentTotal();
    }

    [RelayCommand]
    private void AddAnother()
    {
        var errors = new Dictionary<string, string[]>();

        if (ExpenseDate is null)
        {
            errors[nameof(ExpenseDate)] = ["التاريخ مطلوب."];
        }

        if (string.IsNullOrWhiteSpace(NoteText))
        {
            errors[nameof(NoteText)] = ["البيان / الملاحظة مطلوب."];
        }

        if (!WholeNumberInput.TryParseRequiredPositive(
                AmountText,
                out var amount,
                out var amountError,
                "المبلغ مطلوب",
                "المبلغ يجب أن يكون أكبر من صفر",
                "المبلغ يجب أن يكون عدداً صحيحاً"))
        {
            errors[nameof(AmountText)] = [amountError!];
        }

        if (errors.Count > 0)
        {
            ApplyErrors(errors);
            ErrorMessage = "يرجى تصحيح الحقول المطلوبة.";
            return;
        }

        PendingItems.Add(new PendingHomeExpenseItem(NoteText!.Trim(), amount));
        NoteText = null;
        AmountText = null;
        ApplyErrors(new Dictionary<string, string[]>());
        ErrorMessage = null;
        UpdateCurrentTotal();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (PendingItems.Count == 0)
        {
            AddAnother();

            if (PendingItems.Count == 0)
            {
                return;
            }
        }

        if (ExpenseDate is null)
        {
            ApplyErrors(new Dictionary<string, string[]>
            {
                [nameof(ExpenseDate)] = ["التاريخ مطلوب."]
            });
            ErrorMessage = "التاريخ مطلوب.";
            return;
        }

        await ExecuteBusyAsync(async cancellationToken =>
        {
            var result = await homeExpenseService.SaveExpensesAsync(new SaveHomeExpensesDto
            {
                ExpenseDate = DateOnly.FromDateTime(ExpenseDate.Value.Date),
                Items = PendingItems
                    .Select(item => new SaveHomeExpenseItemDto
                    {
                        Note = item.Note,
                        Amount = item.Amount
                    })
                    .ToList()
            }, cancellationToken);

            if (!result.Succeeded)
            {
                ApplyErrors(result.Errors);
                ErrorMessage = result.ErrorSummary;
                return;
            }

            Close(true);
        });
    }

    [RelayCommand]
    private void Cancel()
    {
        Close(false);
    }

    private void UpdateCurrentTotal()
    {
        CurrentTotalText = WholeNumberInput.Format(PendingItems.Sum(item => item.Amount));
    }
}
