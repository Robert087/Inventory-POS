namespace AutoPartsPOS.Application.HomeExpenses.Dtos;

public sealed class SaveHomeExpensesDto
{
    public DateOnly ExpenseDate { get; init; }

    public IReadOnlyList<SaveHomeExpenseItemDto> Items { get; init; } = [];
}
