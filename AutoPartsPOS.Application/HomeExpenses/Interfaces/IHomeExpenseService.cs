using AutoPartsPOS.Application.Common.Models;
using AutoPartsPOS.Application.HomeExpenses.Dtos;

namespace AutoPartsPOS.Application.HomeExpenses.Interfaces;

public interface IHomeExpenseService
{
    Task<IReadOnlyList<HomeExpenseDaySummaryDto>> SearchSummariesAsync(
        DateOnly? fromDate,
        DateOnly? toExclusive,
        CancellationToken cancellationToken = default);

    Task<decimal> GetDayTotalAsync(DateOnly expenseDate, CancellationToken cancellationToken = default);

    Task<decimal> GetMonthTotalAsync(int year, int month, CancellationToken cancellationToken = default);

    Task<HomeExpenseDayDetailsDto?> GetDetailsAsync(long dayId, CancellationToken cancellationToken = default);

    Task<OperationResult> SaveExpensesAsync(SaveHomeExpensesDto dto, CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteItemAsync(long itemId, CancellationToken cancellationToken = default);
}
