using AutoPartsPOS.Application.Reports.Dtos;
using AutoPartsPOS.Application.Reports.Interfaces;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AutoPartsPOS.WPF.Reports.Services;

public sealed class ReportExportService : IReportExportService
{
    static ReportExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task ExportSalesReportToPdfAsync(SalesReportDto report, string filePath, CancellationToken cancellationToken = default) =>
        GenerateSummaryPdfAsync("تقرير المبيعات", filePath,
        [
            ("من", report.FromDate.ToString()),
            ("إلى", report.ToDate.ToString()),
            ("عدد الفواتير", report.InvoiceCount.ToString()),
            ("إجمالي المبيعات", report.TotalSales.ToString("N2")),
            ("إجمالي الخصم", report.TotalDiscount.ToString("N2")),
            ("صافي المبيعات", report.NetSales.ToString("N2"))
        ], cancellationToken);

    public Task ExportProfitReportToPdfAsync(ProfitReportDto report, string filePath, CancellationToken cancellationToken = default) =>
        GenerateSummaryPdfAsync("تقرير الأرباح", filePath,
        [
            ("من", report.FromDate.ToString()),
            ("إلى", report.ToDate.ToString()),
            ("الإيرادات", report.Revenue.ToString("N2")),
            ("التكلفة", report.Cost.ToString("N2")),
            ("الربح", report.Profit.ToString("N2"))
        ], cancellationToken);

    public Task ExportInventoryReportToPdfAsync(InventoryReportDto report, string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.ContentFromRightToLeft();
                page.Header().Text("تقرير المخزون").FontSize(20).Bold();
                page.Content().Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Text($"إجمالي قيمة المخزون: {report.InventoryValue:N2}");
                    column.Item().Text($"عدد المنتجات منخفضة المخزون: {report.LowStockCount}");
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });
                        AddHeader(table, "الكود", "المنتج", "المخزون", "القيمة", "منخفض");
                        foreach (var item in report.Items)
                        {
                            table.Cell().BorderBottom(1).Padding(4).Text(item.ProductCode);
                            table.Cell().BorderBottom(1).Padding(4).Text(item.ProductNameAr);
                            table.Cell().BorderBottom(1).Padding(4).Text(item.CurrentStock.ToString("N3"));
                            table.Cell().BorderBottom(1).Padding(4).Text(item.InventoryValue.ToString("N2"));
                            table.Cell().BorderBottom(1).Padding(4).Text(item.IsLowStock ? "نعم" : "لا");
                        }
                    });
                });
            });
        }).GeneratePdf(filePath);
        return Task.CompletedTask;
    }

    public Task ExportSalesReportToExcelAsync(SalesReportDto report, string filePath, CancellationToken cancellationToken = default) =>
        GenerateSummaryExcelAsync("المبيعات", filePath,
        [
            ("من", report.FromDate.ToString()),
            ("إلى", report.ToDate.ToString()),
            ("عدد الفواتير", report.InvoiceCount),
            ("إجمالي المبيعات", report.TotalSales),
            ("إجمالي الخصم", report.TotalDiscount),
            ("صافي المبيعات", report.NetSales)
        ], cancellationToken);

    public Task ExportProfitReportToExcelAsync(ProfitReportDto report, string filePath, CancellationToken cancellationToken = default) =>
        GenerateSummaryExcelAsync("الأرباح", filePath,
        [
            ("من", report.FromDate.ToString()),
            ("إلى", report.ToDate.ToString()),
            ("الإيرادات", report.Revenue),
            ("التكلفة", report.Cost),
            ("الربح", report.Profit)
        ], cancellationToken);

    public Task ExportInventoryReportToExcelAsync(InventoryReportDto report, string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("المخزون");
        sheet.RightToLeft = true;
        var headers = new[] { "الكود", "المنتج", "المخزون الحالي", "سعر الشراء", "قيمة المخزون", "الحد الأدنى", "منخفض المخزون" };
        for (var column = 0; column < headers.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = headers[column];
        }
        for (var index = 0; index < report.Items.Count; index++)
        {
            var item = report.Items[index];
            var row = index + 2;
            sheet.Cell(row, 1).Value = item.ProductCode;
            sheet.Cell(row, 2).Value = item.ProductNameAr;
            sheet.Cell(row, 3).Value = item.CurrentStock;
            sheet.Cell(row, 4).Value = item.PurchasePrice;
            sheet.Cell(row, 5).Value = item.InventoryValue;
            sheet.Cell(row, 6).Value = item.MinimumStock;
            sheet.Cell(row, 7).Value = item.IsLowStock ? "نعم" : "لا";
        }
        StyleSheet(sheet);
        workbook.SaveAs(filePath);
        return Task.CompletedTask;
    }

    private static Task GenerateSummaryPdfAsync(string title, string filePath, IReadOnlyList<(string Label, string Value)> rows, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.ContentFromRightToLeft();
            page.Header().Text(title).FontSize(22).Bold();
            page.Content().Column(column =>
            {
                column.Spacing(10);
                foreach (var row in rows)
                {
                    column.Item().Text($"{row.Label}: {row.Value}").FontSize(14);
                }
            });
        })).GeneratePdf(filePath);
        return Task.CompletedTask;
    }

    private static Task GenerateSummaryExcelAsync(string title, string filePath, IReadOnlyList<(string Label, object Value)> rows, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(title);
        sheet.RightToLeft = true;
        for (var index = 0; index < rows.Count; index++)
        {
            sheet.Cell(index + 1, 1).Value = rows[index].Label;
            sheet.Cell(index + 1, 2).Value = XLCellValue.FromObject(rows[index].Value);
        }
        StyleSheet(sheet);
        workbook.SaveAs(filePath);
        return Task.CompletedTask;
    }

    private static void StyleSheet(IXLWorksheet sheet)
    {
        sheet.Row(1).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();
        sheet.RangeUsed()?.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        sheet.RangeUsed()?.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
    }

    private static void AddHeader(TableDescriptor table, params string[] headers)
    {
        foreach (var header in headers)
        {
            table.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).Padding(4).Text(header).Bold();
        }
    }
}
