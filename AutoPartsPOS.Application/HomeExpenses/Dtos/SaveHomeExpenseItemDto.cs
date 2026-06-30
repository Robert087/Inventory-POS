namespace AutoPartsPOS.Application.HomeExpenses.Dtos;

public sealed class SaveHomeExpenseItemDto
{
    public string Note { get; init; } = string.Empty;

    public decimal Amount { get; init; }
}
