using AutoPartsPOS.Application.Common.ViewModels;
using AutoPartsPOS.Application.Reports.Dtos;
using AutoPartsPOS.Application.Reports.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Globalization;

namespace AutoPartsPOS.WPF.Reports.ViewModels;

public sealed partial class ReportsViewModel(
    IReportingService reportingService,
    IReportExportService exportService) : ViewModelBase
{
    public ObservableCollection<InventoryReportItemDto> InventoryItems { get; } = [];
    private readonly List<InventoryReportItemDto> _allInventoryItems = [];

    [ObservableProperty] private DateTime? _fromDate;
    [ObservableProperty] private DateTime? _toDate;
    [ObservableProperty] private SalesReportDto? _salesReport;
    [ObservableProperty] private ProfitReportDto? _profitReport;
    [ObservableProperty] private InventoryReportDto? _inventoryReport;
    [ObservableProperty] private string? _successMessage;
    [ObservableProperty] private string? _searchText;

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Title = "التقارير";
        await LoadAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        FromDate = null;
        ToDate = null;
        await LoadAsync();
    }

    [RelayCommand]
    private void SearchInventory() => ApplyInventoryFilter();

    [RelayCommand]
    private async Task ExportSalesPdfAsync() => await ExportAsync("ملف PDF (*.pdf)|*.pdf", "تقرير-المبيعات.pdf", path => exportService.ExportSalesReportToPdfAsync(SalesReport!, path), SalesReport is not null);

    [RelayCommand]
    private async Task ExportSalesExcelAsync() => await ExportAsync("ملف Excel (*.xlsx)|*.xlsx", "تقرير-المبيعات.xlsx", path => exportService.ExportSalesReportToExcelAsync(SalesReport!, path), SalesReport is not null);

    [RelayCommand]
    private async Task ExportProfitPdfAsync() => await ExportAsync("ملف PDF (*.pdf)|*.pdf", "تقرير-الأرباح.pdf", path => exportService.ExportProfitReportToPdfAsync(ProfitReport!, path), ProfitReport is not null);

    [RelayCommand]
    private async Task ExportProfitExcelAsync() => await ExportAsync("ملف Excel (*.xlsx)|*.xlsx", "تقرير-الأرباح.xlsx", path => exportService.ExportProfitReportToExcelAsync(ProfitReport!, path), ProfitReport is not null);

    [RelayCommand]
    private async Task ExportInventoryPdfAsync() => await ExportAsync("ملف PDF (*.pdf)|*.pdf", "تقرير-المخزون.pdf", path => exportService.ExportInventoryReportToPdfAsync(InventoryReport!, path), InventoryReport is not null);

    [RelayCommand]
    private async Task ExportInventoryExcelAsync() => await ExportAsync("ملف Excel (*.xlsx)|*.xlsx", "تقرير-المخزون.xlsx", path => exportService.ExportInventoryReportToExcelAsync(InventoryReport!, path), InventoryReport is not null);

    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await ExecuteBusyAsync(async token =>
            {
                SuccessMessage = null;
                var from = FromDate is null ? DateOnly.MinValue : DateOnly.FromDateTime(FromDate.Value.Date);
                var to = ToDate is null ? DateOnly.MaxValue : DateOnly.FromDateTime(ToDate.Value.Date);
                if (from > to)
                {
                    (from, to) = (to, from);
                }

                SalesReport = await reportingService.GetSalesReportAsync(from, to, token);
                ProfitReport = await reportingService.GetProfitReportAsync(from, to, token);
                InventoryReport = await reportingService.GetInventoryReportAsync(token);
                _allInventoryItems.Clear();

                foreach (var item in InventoryReport.Items)
                {
                    _allInventoryItems.Add(item);
                }

                ApplyInventoryFilter();

                ErrorMessage = null;
            }, cancellationToken);
        }
        catch (Exception exception)
        {
            ErrorMessage = $"تعذر تحميل التقارير: {exception.Message}";
        }
    }

    private async Task ExportAsync(string filter, string fileName, Func<string, Task> export, bool canExport)
    {
        if (!canExport)
        {
            return;
        }

        var dialog = new SaveFileDialog { Filter = filter, FileName = fileName, AddExtension = true };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            await export(dialog.FileName);
            SuccessMessage = $"تم حفظ التقرير: {dialog.FileName}";
            ErrorMessage = null;
        }
        catch (Exception exception)
        {
            ErrorMessage = $"تعذر تصدير التقرير: {exception.Message}";
        }
    }

    partial void OnSearchTextChanged(string? value) => ApplyInventoryFilter();

    private void ApplyInventoryFilter()
    {
        InventoryItems.Clear();

        foreach (var item in _allInventoryItems.Where(MatchesInventorySearch))
        {
            InventoryItems.Add(item);
        }
    }

    private bool MatchesInventorySearch(InventoryReportItemDto item)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        var term = SearchText.Trim();
        return Contains(item.ProductCode, term)
            || Contains(item.ProductNameAr, term)
            || item.CurrentStock.ToString("F0", CultureInfo.CurrentCulture).Contains(term, StringComparison.CurrentCultureIgnoreCase)
            || item.CurrentAverageCost.ToString("N4", CultureInfo.CurrentCulture).Contains(term, StringComparison.CurrentCultureIgnoreCase)
            || item.InventoryValue.ToString("F0", CultureInfo.CurrentCulture).Contains(term, StringComparison.CurrentCultureIgnoreCase);
    }

    private static bool Contains(string? source, string term) =>
        !string.IsNullOrWhiteSpace(source) && source.Contains(term, StringComparison.CurrentCultureIgnoreCase);
}
