namespace AutoPartsPOS.WPF.HomeExpenses.Models;

public sealed class PendingHomeExpenseItem(string note, decimal amount)
{
    public string Note { get; } = note;

    public decimal Amount { get; } = amount;
}
