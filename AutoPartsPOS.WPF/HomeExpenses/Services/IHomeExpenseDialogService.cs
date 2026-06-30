namespace AutoPartsPOS.WPF.HomeExpenses.Services;

public interface IHomeExpenseDialogService
{
    Task<bool> ShowAddDialogAsync(DateOnly? presetDate = null);

    Task<bool> ShowDetailsDialogAsync(long dayId);

    bool ShowDeleteConfirmationDialog();
}
