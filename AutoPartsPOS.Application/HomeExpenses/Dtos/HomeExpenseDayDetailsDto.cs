namespace AutoPartsPOS.Application.HomeExpenses.Dtos;

public sealed record HomeExpenseDayDetailsDto(
    long Id,
    DateOnly ExpenseDate,
    decimal TotalAmount,
    IReadOnlyList<HomeExpenseItemDto> Items);
