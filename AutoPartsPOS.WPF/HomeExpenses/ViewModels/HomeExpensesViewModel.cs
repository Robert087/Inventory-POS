using AutoPartsPOS.Application.Common.ViewModels;
using AutoPartsPOS.Application.HomeExpenses.Dtos;
using AutoPartsPOS.Application.HomeExpenses.Interfaces;
using AutoPartsPOS.WPF.HomeExpenses.Services;
using AutoPartsPOS.WPF.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AutoPartsPOS.WPF.HomeExpenses.ViewModels;

public sealed partial class HomeExpensesViewModel(
    IHomeExpenseService homeExpenseService,
    IHomeExpenseDialogService dialogService) : ViewModelBase
{
    private bool _isDailyFilterActive;
    private bool _isMonthlyFilterActive;

    public ObservableCollection<HomeExpenseDaySummaryDto> ExpenseDays { get; } = [];

    public IReadOnlyList<MonthOption> Months { get; } =
    [
        new(1, "يناير"), new(2, "فبراير"), new(3, "مارس"), new(4, "أبريل"),
        new(5, "مايو"), new(6, "يونيو"), new(7, "يوليو"), new(8, "أغسطس"),
        new(9, "سبتمبر"), new(10, "أكتوبر"), new(11, "نوفمبر"), new(12, "ديسمبر")
    ];

    public IReadOnlyList<int> Years { get; } = Enumerable.Range(DateTime.Today.Year - 10, 12).Reverse().ToList();

    [ObservableProperty]
    private decimal _todayExpensesTotal;

    [ObservableProperty]
    private decimal _monthExpensesTotal;

    [ObservableProperty]
    private DateTime? _dailyExpenseDate = DateTime.Today;

    [ObservableProperty]
    private int _selectedMonth = DateTime.Today.Month;

    [ObservableProperty]
    private int _selectedYear = DateTime.Today.Year;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ViewCommand))]
    private HomeExpenseDaySummaryDto? _selectedExpenseDay;

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Title = "مصاريف المنزل";
        await LoadAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task ApplyDailyFilterAsync()
    {
        if (DailyExpenseDate is null)
        {
            ErrorMessage = "يرجى اختيار تاريخ.";
            return;
        }

        ErrorMessage = null;
        _isDailyFilterActive = true;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ResetDailyFilterAsync()
    {
        _isDailyFilterActive = false;
        DailyExpenseDate = DateTime.Today;
        ErrorMessage = null;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ApplyMonthlyFilterAsync()
    {
        ErrorMessage = null;
        _isMonthlyFilterActive = true;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ResetMonthlyFilterAsync()
    {
        _isMonthlyFilterActive = false;
        SelectedMonth = DateTime.Today.Month;
        SelectedYear = DateTime.Today.Year;
        ErrorMessage = null;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (await dialogService.ShowAddDialogAsync())
        {
            await LoadAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedExpenseDay))]
    private async Task ViewAsync()
    {
        if (SelectedExpenseDay is null)
        {
            return;
        }

        await ViewExpenseDayAsync(SelectedExpenseDay);
    }

    [RelayCommand]
    private async Task ViewRowAsync(HomeExpenseDaySummaryDto? expenseDay)
    {
        if (expenseDay is null)
        {
            return;
        }

        await ViewExpenseDayAsync(expenseDay);
    }

    private async Task ViewExpenseDayAsync(HomeExpenseDaySummaryDto expenseDay)
    {
        if (await dialogService.ShowDetailsDialogAsync(expenseDay.Id))
        {
            await LoadAsync();
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await ExecuteBusyAsync(async token =>
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var dailyCardDate = _isDailyFilterActive
                ? DateOnly.FromDateTime(DailyExpenseDate!.Value.Date)
                : today;

            var monthlyCardYear = _isMonthlyFilterActive ? SelectedYear : today.Year;
            var monthlyCardMonth = _isMonthlyFilterActive ? SelectedMonth : today.Month;

            TodayExpensesTotal = await homeExpenseService.GetDayTotalAsync(dailyCardDate, token);
            MonthExpensesTotal = await homeExpenseService.GetMonthTotalAsync(monthlyCardYear, monthlyCardMonth, token);

            ExpenseDays.Clear();

            DateOnly? fromDate = null;
            DateOnly? toExclusive = null;

            if (_isDailyFilterActive)
            {
                fromDate = DateOnly.FromDateTime(DailyExpenseDate!.Value.Date);
                toExclusive = fromDate.Value.AddDays(1);
            }
            else if (_isMonthlyFilterActive)
            {
                fromDate = new DateOnly(SelectedYear, SelectedMonth, 1);
                toExclusive = fromDate.Value.AddMonths(1);
            }

            foreach (var day in await homeExpenseService.SearchSummariesAsync(fromDate, toExclusive, token))
            {
                ExpenseDays.Add(day);
            }
        }, cancellationToken);
    }

    private bool HasSelectedExpenseDay()
    {
        return SelectedExpenseDay is not null;
    }
}
