using AutoPartsPOS.Domain.Common;

namespace AutoPartsPOS.Domain.HomeExpenses;

public sealed class HomeExpenseDay : AuditableEntity
{
    public DateOnly ExpenseDate { get; set; }

    public decimal TotalAmount { get; set; }

    public ICollection<HomeExpenseItem> Items { get; set; } = [];
}
