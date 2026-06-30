using AutoPartsPOS.Application.Analytics.Dtos;
using AutoPartsPOS.Application.Common.ViewModels;
using AutoPartsPOS.Application.Dashboard.Interfaces;
using AutoPartsPOS.Application.Insights.Dtos;
using AutoPartsPOS.Application.Purchases.Dtos;
using AutoPartsPOS.Application.Purchases.Interfaces;
using AutoPartsPOS.Application.Sales.Dtos;
using AutoPartsPOS.Application.Sales.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AutoPartsPOS.WPF.ViewModels;

public sealed partial class HomeViewModel(
    IDashboardService dashboardService,
    ISalesInvoiceService salesInvoiceService,
    IPurchaseInvoiceService purchaseInvoiceService) : ViewModelBase
{
    public ObservableCollection<TopSellingProductDto> TopSellingProducts { get; } = [];
    public ObservableCollection<LowStockInsightDto> LowStockProducts { get; } = [];
    public ObservableCollection<SlowMovingProductDto> SlowMovingProducts { get; } = [];
    public ObservableCollection<SalesInvoiceListDto> RecentSales { get; } = [];
    public ObservableCollection<PurchaseInvoiceListDto> RecentPurchases { get; } = [];
    public IReadOnlyList<MonthOption> Months { get; } =
    [
        new(1, "يناير"), new(2, "فبراير"), new(3, "مارس"), new(4, "أبريل"),
        new(5, "مايو"), new(6, "يونيو"), new(7, "يوليو"), new(8, "أغسطس"),
        new(9, "سبتمبر"), new(10, "أكتوبر"), new(11, "نوفمبر"), new(12, "ديسمبر")
    ];
    public IReadOnlyList<int> Years { get; } = Enumerable.Range(DateTime.Today.Year - 10, 12).Reverse().ToList();

    [ObservableProperty] private decimal _todaySales;
    [ObservableProperty] private decimal _currentMonthSales;
    [ObservableProperty] private int _invoiceCount;
    [ObservableProperty] private decimal _inventoryValue;
    [ObservableProperty] private decimal _netProfit;
    [ObservableProperty] private int _lowStockCount;
    [ObservableProperty] private DateTime? _dailySalesDate = DateTime.Today;
    [ObservableProperty] private int _selectedMonth = DateTime.Today.Month;
    [ObservableProperty] private int _selectedYear = DateTime.Today.Year;

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Title = "لوحة التحكم";
        await LoadAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task ApplyDailySalesFilterAsync()
    {
        var selectedDate = DateOnly.FromDateTime(DailySalesDate ?? DateTime.Today);
        await LoadDailySalesAsync(selectedDate);
    }

    [RelayCommand]
    private async Task ResetDailySalesFilterAsync()
    {
        DailySalesDate = DateTime.Today;
        await LoadDailySalesAsync(DateOnly.FromDateTime(DateTime.Today));
    }

    [RelayCommand]
    private async Task ApplyMonthlyFilterAsync() => await LoadMonthlyStatisticsAsync(SelectedYear, SelectedMonth);

    [RelayCommand]
    private async Task ResetMonthlyFilterAsync()
    {
        SelectedMonth = DateTime.Today.Month;
        SelectedYear = DateTime.Today.Year;
        await LoadMonthlyStatisticsAsync(SelectedYear, SelectedMonth);
    }

    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await ExecuteBusyAsync(async token =>
            {
                var dashboard = await dashboardService.LoadAsync(token);
                TodaySales = dashboard.TodaySales;
                CurrentMonthSales = dashboard.CurrentMonthSales;
                InvoiceCount = dashboard.InvoiceCount;
                InventoryValue = dashboard.InventoryValue;
                NetProfit = dashboard.NetProfit;
                LowStockCount = dashboard.LowStockCount;

                Replace(TopSellingProducts, dashboard.TopSellingProducts);
                Replace(LowStockProducts, dashboard.LowStockProducts);
                Replace(SlowMovingProducts, dashboard.SlowMovingProducts);

                var recentSales = (await salesInvoiceService.SearchAsync(null, null, null, token)).Take(5);
                var recentPurchases = (await purchaseInvoiceService.SearchAsync(null, token)).Take(5);
                Replace(RecentSales, recentSales);
                Replace(RecentPurchases, recentPurchases);
            }, cancellationToken);
        }
        catch (Exception exception)
        {
            ErrorMessage = $"تعذر تحميل لوحة التحكم: {exception.Message}";
        }
    }

    private async Task LoadDailySalesAsync(DateOnly date)
    {
        try
        {
            await ExecuteBusyAsync(async token =>
            {
                TodaySales = await dashboardService.GetDailySalesAsync(date, token);
                ErrorMessage = null;
            });
        }
        catch (Exception exception)
        {
            ErrorMessage = $"تعذر تحميل مبيعات اليوم: {exception.Message}";
        }
    }

    private async Task LoadMonthlyStatisticsAsync(int year, int month)
    {
        try
        {
            await ExecuteBusyAsync(async token =>
            {
                var statistics = await dashboardService.GetMonthlyStatisticsAsync(year, month, token);
                CurrentMonthSales = statistics.Sales;
                InvoiceCount = statistics.InvoiceCount;
                NetProfit = statistics.NetProfit;
                ErrorMessage = null;
            });
        }
        catch (Exception exception)
        {
            ErrorMessage = $"تعذر تحميل إحصائيات الشهر: {exception.Message}";
        }
    }

    private static void Replace<T>(ObservableCollection<T> collection, IEnumerable<T> values)
    {
        collection.Clear();
        foreach (var value in values)
        {
            collection.Add(value);
        }
    }
}

public sealed record MonthOption(int Value, string Name);
