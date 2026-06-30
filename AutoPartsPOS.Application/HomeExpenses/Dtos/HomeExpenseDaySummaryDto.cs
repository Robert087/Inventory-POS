namespace AutoPartsPOS.Application.HomeExpenses.Dtos;

public sealed record HomeExpenseDaySummaryDto(
    long Id,
    DateOnly ExpenseDate,
    decimal TotalAmount);
