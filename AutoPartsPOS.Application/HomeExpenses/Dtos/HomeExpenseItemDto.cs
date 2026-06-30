namespace AutoPartsPOS.Application.HomeExpenses.Dtos;

public sealed record HomeExpenseItemDto(
    long Id,
    string Note,
    decimal Amount);
