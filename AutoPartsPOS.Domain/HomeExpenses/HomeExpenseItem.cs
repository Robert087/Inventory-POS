using AutoPartsPOS.Domain.Common;

namespace AutoPartsPOS.Domain.HomeExpenses;

public sealed class HomeExpenseItem : AuditableEntity
{
    public long HomeExpenseDayId { get; set; }

    public HomeExpenseDay? HomeExpenseDay { get; set; }

    public string Note { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}
