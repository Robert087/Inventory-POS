using AutoPartsPOS.Application.Analytics.Dtos;
using AutoPartsPOS.Application.Common.ViewModels;
using AutoPartsPOS.Application.Dashboard.Interfaces;
using AutoPartsPOS.Application.Insights.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AutoPartsPOS.WPF.ViewModels;

public sealed partial class HomeViewModel(IDashboardService dashboardService) : ViewModelBase
{
    public ObservableCollection<TopSellingProductDto> TopSellingProducts { get; } = [];
    public ObservableCollection<LowStockInsightDto> LowStockProducts { get; } = [];
    public ObservableCollection<SlowMovingProductDto> SlowMovingProducts { get; } = [];
    public ObservableCollection<ReorderSuggestionDto> ReorderSuggestions { get; } = [];

    [ObservableProperty] private decimal _todaySales;
    [ObservableProperty] private decimal _currentMonthSales;
    [ObservableProperty] private int _invoiceCount;
    [ObservableProperty] private decimal _inventoryValue;
    [ObservableProperty] private int _lowStockCount;

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Title = "لوحة التحكم";
        await LoadAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

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
                LowStockCount = dashboard.LowStockCount;
                Replace(TopSellingProducts, dashboard.TopSellingProducts);
                Replace(LowStockProducts, dashboard.LowStockProducts);
                Replace(SlowMovingProducts, dashboard.SlowMovingProducts);
                Replace(ReorderSuggestions, dashboard.ReorderSuggestions);
            }, cancellationToken);
        }
        catch (Exception exception)
        {
            ErrorMessage = $"تعذر تحميل لوحة التحكم: {exception.Message}";
        }
    }

    private static void Replace<T>(ObservableCollection<T> collection, IEnumerable<T> values)
    {
        collection.Clear();
        foreach (var value in values) collection.Add(value);
    }
}
