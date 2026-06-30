using AutoPartsPOS.Application.Common.Interfaces;
using AutoPartsPOS.Application.Common.Models;
using AutoPartsPOS.Application.HomeExpenses.Dtos;
using AutoPartsPOS.Application.HomeExpenses.Interfaces;
using AutoPartsPOS.Domain.HomeExpenses;
using System.Text;

namespace AutoPartsPOS.Application.HomeExpenses.Services;

public sealed class HomeExpenseService(
    IHomeExpenseRepository homeExpenseRepository,
    IUnitOfWork unitOfWork) : IHomeExpenseService
{
    public Task<IReadOnlyList<HomeExpenseDaySummaryDto>> SearchSummariesAsync(
        DateOnly? fromDate,
        DateOnly? toExclusive,
        CancellationToken cancellationToken = default)
    {
        return homeExpenseRepository.SearchSummariesAsync(fromDate, toExclusive, cancellationToken);
    }

    public Task<decimal> GetDayTotalAsync(DateOnly expenseDate, CancellationToken cancellationToken = default)
    {
        return homeExpenseRepository.GetDayTotalAsync(expenseDate, cancellationToken);
    }

    public Task<decimal> GetMonthTotalAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        return homeExpenseRepository.GetMonthTotalAsync(year, month, cancellationToken);
    }

    public Task<HomeExpenseDayDetailsDto?> GetDetailsAsync(long dayId, CancellationToken cancellationToken = default)
    {
        return homeExpenseRepository.GetDetailsByIdAsync(dayId, cancellationToken);
    }

    public async Task<OperationResult> SaveExpensesAsync(SaveHomeExpensesDto dto, CancellationToken cancellationToken = default)
    {
        var errors = ValidateSave(dto);

        if (errors.Count > 0)
        {
            return OperationResult.Failure(errors);
        }

        var day = await homeExpenseRepository.GetDayWithItemsByDateAsync(dto.ExpenseDate, cancellationToken);

        if (day is null)
        {
            day = new HomeExpenseDay
            {
                ExpenseDate = dto.ExpenseDate,
                TotalAmount = 0
            };

            await homeExpenseRepository.AddDayAsync(day, cancellationToken);
        }

        foreach (var itemDto in dto.Items)
        {
            day.Items.Add(new HomeExpenseItem
            {
                Note = Normalize(itemDto.Note),
                Amount = itemDto.Amount
            });
        }

        day.TotalAmount = day.Items.Sum(item => item.Amount);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteItemAsync(long itemId, CancellationToken cancellationToken = default)
    {
        var item = await homeExpenseRepository.GetItemByIdAsync(itemId, cancellationToken);

        if (item?.HomeExpenseDay is null)
        {
            return OperationResult.Failure(new Dictionary<string, List<string>>
            {
                [string.Empty] = ["البند غير موجود."]
            });
        }

        var day = item.HomeExpenseDay;
        await homeExpenseRepository.DeleteItemAsync(item, cancellationToken);
        day.Items.Remove(item);
        day.TotalAmount = day.Items.Sum(existingItem => existingItem.Amount);

        if (day.Items.Count == 0)
        {
            await homeExpenseRepository.DeleteDayAsync(day, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    private static Dictionary<string, List<string>> ValidateSave(SaveHomeExpensesDto dto)
    {
        var errors = new Dictionary<string, List<string>>();

        if (dto.ExpenseDate == default)
        {
            AddError(errors, nameof(SaveHomeExpensesDto.ExpenseDate), "التاريخ مطلوب.");
        }

        if (dto.Items.Count == 0)
        {
            AddError(errors, string.Empty, "يجب إضافة مصروف واحد على الأقل.");
        }

        for (var index = 0; index < dto.Items.Count; index++)
        {
            var item = dto.Items[index];
            var note = Normalize(item.Note);

            if (string.IsNullOrWhiteSpace(note))
            {
                AddError(errors, $"Items[{index}].Note", "البيان / الملاحظة مطلوب.");
            }

            if (item.Amount <= 0)
            {
                AddError(errors, $"Items[{index}].Amount", "المبلغ يجب أن يكون أكبر من صفر.");
            }
            else if (item.Amount != decimal.Truncate(item.Amount))
            {
                AddError(errors, $"Items[{index}].Amount", "المبلغ يجب أن يكون عدداً صحيحاً.");
            }
        }

        return errors;
    }

    private static string Normalize(string value)
    {
        return value.Trim().Normalize(NormalizationForm.FormC);
    }

    private static void AddError(Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var messages))
        {
            messages = [];
            errors[key] = messages;
        }

        messages.Add(message);
    }
}
