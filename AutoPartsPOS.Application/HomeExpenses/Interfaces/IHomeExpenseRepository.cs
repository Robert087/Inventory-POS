using AutoPartsPOS.Application.HomeExpenses.Dtos;
using AutoPartsPOS.Domain.HomeExpenses;

namespace AutoPartsPOS.Application.HomeExpenses.Interfaces;

public interface IHomeExpenseRepository
{
    Task<IReadOnlyList<HomeExpenseDaySummaryDto>> SearchSummariesAsync(
        DateOnly? fromDate,
        DateOnly? toExclusive,
        CancellationToken cancellationToken = default);

    Task<decimal> GetDayTotalAsync(DateOnly expenseDate, CancellationToken cancellationToken = default);

    Task<decimal> GetMonthTotalAsync(int year, int month, CancellationToken cancellationToken = default);

    Task<HomeExpenseDayDetailsDto?> GetDetailsByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<HomeExpenseDay?> GetDayWithItemsByDateAsync(
        DateOnly expenseDate,
        CancellationToken cancellationToken = default);

    Task<HomeExpenseDay?> GetDayWithItemsByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task AddDayAsync(HomeExpenseDay day, CancellationToken cancellationToken = default);

    Task AddItemAsync(HomeExpenseItem item, CancellationToken cancellationToken = default);

    Task<HomeExpenseItem?> GetItemByIdAsync(long itemId, CancellationToken cancellationToken = default);

    Task DeleteItemAsync(HomeExpenseItem item, CancellationToken cancellationToken = default);

    Task DeleteDayAsync(HomeExpenseDay day, CancellationToken cancellationToken = default);
}
