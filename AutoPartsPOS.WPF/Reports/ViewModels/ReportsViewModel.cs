using AutoPartsPOS.Application.Common.ViewModels;
using AutoPartsPOS.Application.Reports.Dtos;
using AutoPartsPOS.Application.Reports.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace AutoPartsPOS.WPF.Reports.ViewModels;

public sealed partial class ReportsViewModel(
    IReportingService reportingService,
    IReportExportService exportService) : ViewModelBase
{
    public ObservableCollection<InventoryReportItemDto> InventoryItems { get; } = [];

    [ObservableProperty] private DateTime? _fromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    [ObservableProperty] private DateTime? _toDate = DateTime.Today;
    [ObservableProperty] private SalesReportDto? _salesReport;
    [ObservableProperty] private ProfitReportDto? _profitReport;
    [ObservableProperty] private InventoryReportDto? _inventoryReport;
    [ObservableProperty] private string? _successMessage;

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Title = "التقارير";
        await LoadAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task ExportSalesPdfAsync() => await ExportAsync("PDF (*.pdf)|*.pdf", "تقرير-المبيعات.pdf", path => exportService.ExportSalesReportToPdfAsync(SalesReport!, path), SalesReport is not null);

    [RelayCommand]
    private async Task ExportSalesExcelAsync() => await ExportAsync("Excel (*.xlsx)|*.xlsx", "تقرير-المبيعات.xlsx", path => exportService.ExportSalesReportToExcelAsync(SalesReport!, path), SalesReport is not null);

    [RelayCommand]
    private async Task ExportProfitPdfAsync() => await ExportAsync("PDF (*.pdf)|*.pdf", "تقرير-الأرباح.pdf", path => exportService.ExportProfitReportToPdfAsync(ProfitReport!, path), ProfitReport is not null);

    [RelayCommand]
    private async Task ExportProfitExcelAsync() => await ExportAsync("Excel (*.xlsx)|*.xlsx", "تقرير-الأرباح.xlsx", path => exportService.ExportProfitReportToExcelAsync(ProfitReport!, path), ProfitReport is not null);

    [RelayCommand]
    private async Task ExportInventoryPdfAsync() => await ExportAsync("PDF (*.pdf)|*.pdf", "تقرير-المخزون.pdf", path => exportService.ExportInventoryReportToPdfAsync(InventoryReport!, path), InventoryReport is not null);

    [RelayCommand]
    private async Task ExportInventoryExcelAsync() => await ExportAsync("Excel (*.xlsx)|*.xlsx", "تقرير-المخزون.xlsx", path => exportService.ExportInventoryReportToExcelAsync(InventoryReport!, path), InventoryReport is not null);

    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await ExecuteBusyAsync(async token =>
            {
                SuccessMessage = null;
                var from = DateOnly.FromDateTime(FromDate ?? DateTime.Today);
                var to = DateOnly.FromDateTime(ToDate ?? DateTime.Today);
                if (from > to) (from, to) = (to, from);

                SalesReport = await reportingService.GetSalesReportAsync(from, to, token);
                ProfitReport = await reportingService.GetProfitReportAsync(from, to, token);
                InventoryReport = await reportingService.GetInventoryReportAsync(token);
                InventoryItems.Clear();
                foreach (var item in InventoryReport.Items) InventoryItems.Add(item);
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
        if (!canExport) return;
        var dialog = new SaveFileDialog { Filter = filter, FileName = fileName, AddExtension = true };
        if (dialog.ShowDialog() != true) return;

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
}
