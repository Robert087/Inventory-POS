using AutoPartsPOS.Application.HomeExpenses.Dtos;
using AutoPartsPOS.Application.HomeExpenses.Interfaces;
using AutoPartsPOS.Domain.HomeExpenses;
using AutoPartsPOS.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsPOS.Persistence.HomeExpenses;

public sealed class HomeExpenseRepository(AppDbContext dbContext) : IHomeExpenseRepository
{
    public async Task<IReadOnlyList<HomeExpenseDaySummaryDto>> SearchSummariesAsync(
        DateOnly? fromDate,
        DateOnly? toExclusive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<HomeExpenseDay>().AsNoTracking();

        if (fromDate is not null)
        {
            query = query.Where(day => day.ExpenseDate >= fromDate.Value);
        }

        if (toExclusive is not null)
        {
            query = query.Where(day => day.ExpenseDate < toExclusive.Value);
        }

        return await query
            .OrderByDescending(day => day.ExpenseDate)
            .Select(day => new HomeExpenseDaySummaryDto(day.Id, day.ExpenseDate, day.TotalAmount))
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetDayTotalAsync(DateOnly expenseDate, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<HomeExpenseDay>()
            .AsNoTracking()
            .Where(day => day.ExpenseDate == expenseDate)
            .Select(day => day.TotalAmount)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<decimal> GetMonthTotalAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var monthStart = new DateOnly(year, month, 1);
        var monthEndExclusive = monthStart.AddMonths(1);

        return await dbContext.Set<HomeExpenseDay>()
            .AsNoTracking()
            .Where(day => day.ExpenseDate >= monthStart && day.ExpenseDate < monthEndExclusive)
            .SumAsync(day => day.TotalAmount, cancellationToken);
    }

    public async Task<HomeExpenseDayDetailsDto?> GetDetailsByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<HomeExpenseDay>()
            .AsNoTracking()
            .Where(day => day.Id == id)
            .Select(day => new HomeExpenseDayDetailsDto(
                day.Id,
                day.ExpenseDate,
                day.TotalAmount,
                day.Items
                    .OrderBy(item => item.Id)
                    .Select(item => new HomeExpenseItemDto(item.Id, item.Note, item.Amount))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<HomeExpenseDay?> GetDayWithItemsByDateAsync(
        DateOnly expenseDate,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Set<HomeExpenseDay>()
            .Include(day => day.Items)
            .FirstOrDefaultAsync(day => day.ExpenseDate == expenseDate, cancellationToken);
    }

    public Task<HomeExpenseDay?> GetDayWithItemsByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Set<HomeExpenseDay>()
            .Include(day => day.Items)
            .FirstOrDefaultAsync(day => day.Id == id, cancellationToken);
    }

    public async Task AddDayAsync(HomeExpenseDay day, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<HomeExpenseDay>().AddAsync(day, cancellationToken);
    }

    public async Task AddItemAsync(HomeExpenseItem item, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<HomeExpenseItem>().AddAsync(item, cancellationToken);
    }

    public Task<HomeExpenseItem?> GetItemByIdAsync(long itemId, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<HomeExpenseItem>()
            .Include(item => item.HomeExpenseDay!)
            .ThenInclude(day => day.Items)
            .FirstOrDefaultAsync(item => item.Id == itemId, cancellationToken);
    }

    public Task DeleteItemAsync(HomeExpenseItem item, CancellationToken cancellationToken = default)
    {
        dbContext.Set<HomeExpenseItem>().Remove(item);
        return Task.CompletedTask;
    }

    public Task DeleteDayAsync(HomeExpenseDay day, CancellationToken cancellationToken = default)
    {
        dbContext.Set<HomeExpenseDay>().Remove(day);
        return Task.CompletedTask;
    }
}
