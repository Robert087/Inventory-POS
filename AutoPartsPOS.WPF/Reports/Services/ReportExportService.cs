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
        GenerateSummaryPdfAsync(
            "تقرير المبيعات",
            GetPeriodText(report.FromDate, report.ToDate),
            filePath,
            [
                ("عدد الفواتير", report.InvoiceCount.ToString()),
                ("إجمالي المبيعات", report.TotalSales.ToString("F0")),
                ("إجمالي الخصم", report.TotalDiscount.ToString("F0")),
                ("صافي المبيعات", report.NetSales.ToString("F0"))
            ],
            cancellationToken);

    public Task ExportProfitReportToPdfAsync(ProfitReportDto report, string filePath, CancellationToken cancellationToken = default) =>
        GenerateSummaryPdfAsync(
            "تقرير الأرباح",
            GetPeriodText(report.FromDate, report.ToDate),
            filePath,
            [
                ("الإيرادات", report.Revenue.ToString("F0")),
                ("التكلفة", report.Cost.ToString("F0")),
                ("صافي الربح", report.Profit.ToString("F0"))
            ],
            cancellationToken);

    public Task ExportInventoryReportToPdfAsync(InventoryReportDto report, string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                ConfigurePage(page);
                AddReportHeader(page, "تقرير المخزون", "كل الفترات");

                page.Content().PaddingVertical(18).Column(column =>
                {
                    column.Spacing(14);
                    column.Item().Text("ملخص التقرير").FontSize(15).Bold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });
                        AddHeader(table, "البيان", "القيمة");
                        AddSummaryRow(table, "إجمالي كمية المخزون", report.TotalStockQuantity.ToString("F0"));
                        AddSummaryRow(table, "قيمة المخزون", report.InventoryValue.ToString("F0"));
                        AddSummaryRow(table, "عدد المنتجات منخفضة المخزون", report.LowStockCount.ToString());
                    });

                    column.Item().PaddingTop(4).Text("تفاصيل المخزون").FontSize(15).Bold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(2.2f);
                            columns.RelativeColumn();
                            columns.RelativeColumn(1.1f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn();
                            columns.RelativeColumn(0.8f);
                        });
                        AddHeader(table, "الكود", "المنتج", "المخزون", "متوسط التكلفة", "قيمة المخزون", "الحد الأدنى", "منخفض");
                        foreach (var item in report.Items)
                        {
                            AddBodyCell(table, item.ProductCode);
                            AddBodyCell(table, item.ProductNameAr);
                            AddBodyCell(table, item.CurrentStock.ToString("F0"));
                            AddBodyCell(table, item.CurrentAverageCost.ToString("F0"));
                            AddBodyCell(table, item.InventoryValue.ToString("F0"));
                            AddBodyCell(table, item.MinimumStock.ToString("F0"));
                            AddBodyCell(table, item.IsLowStock ? "نعم" : "لا");
                        }
                    });
                });

                AddFooter(page);
            });
        }).GeneratePdf(filePath);
        return Task.CompletedTask;
    }

    public Task ExportSalesReportToExcelAsync(SalesReportDto report, string filePath, CancellationToken cancellationToken = default) =>
        GenerateSummaryExcelAsync("المبيعات", filePath,
        [
            ("الفترة", GetPeriodText(report.FromDate, report.ToDate)),
            ("عدد الفواتير", report.InvoiceCount),
            ("إجمالي المبيعات", report.TotalSales),
            ("إجمالي الخصم", report.TotalDiscount),
            ("صافي المبيعات", report.NetSales)
        ], cancellationToken);

    public Task ExportProfitReportToExcelAsync(ProfitReportDto report, string filePath, CancellationToken cancellationToken = default) =>
        GenerateSummaryExcelAsync("الأرباح", filePath,
        [
            ("الفترة", GetPeriodText(report.FromDate, report.ToDate)),
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
            sheet.Cell(row, 4).Value = item.CurrentAverageCost;
            sheet.Cell(row, 5).Value = item.InventoryValue;
            sheet.Cell(row, 6).Value = item.MinimumStock;
            sheet.Cell(row, 7).Value = item.IsLowStock ? "نعم" : "لا";
        }
        StyleSheet(sheet);
        workbook.SaveAs(filePath);
        return Task.CompletedTask;
    }

    private static Task GenerateSummaryPdfAsync(
        string title,
        string period,
        string filePath,
        IReadOnlyList<(string Label, string Value)> rows,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            ConfigurePage(page);
            AddReportHeader(page, title, period);
            page.Content().PaddingVertical(22).Column(column =>
            {
                column.Spacing(14);
                column.Item().Text("ملخص التقرير").FontSize(16).Bold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });
                    AddHeader(table, "البيان", "القيمة");
                    foreach (var row in rows)
                    {
                        AddSummaryRow(table, row.Label, row.Value);
                    }
                });
            });
            AddFooter(page);
        })).GeneratePdf(filePath);
        return Task.CompletedTask;
    }

    private static void ConfigurePage(PageDescriptor page)
    {
        page.Margin(32);
        page.ContentFromRightToLeft();
        page.DefaultTextStyle(style => style.FontSize(11));
    }

    private static void AddReportHeader(PageDescriptor page, string title, string period)
    {
        page.Header().Column(header =>
        {
            header.Spacing(5);
            header.Item().AlignRight().Text(title).FontSize(22).Bold().FontColor(Colors.Blue.Darken2);
            header.Item().AlignRight().Text($"الفترة: {period}").FontSize(11).FontColor(Colors.Grey.Darken2);
            header.Item().AlignRight().Text($"تاريخ الإنشاء: {DateTime.Now:yyyy/MM/dd HH:mm}").FontSize(10).FontColor(Colors.Grey.Darken1);
            header.Item().PaddingTop(7).LineHorizontal(1).LineColor(Colors.Blue.Lighten2);
        });
    }

    private static void AddFooter(PageDescriptor page)
    {
        page.Footer().AlignCenter().Text(text =>
        {
            text.Span("صفحة ").FontSize(9).FontColor(Colors.Grey.Darken1);
            text.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Darken1);
        });
    }

    private static string GetPeriodText(DateOnly fromDate, DateOnly toDate) =>
        fromDate == DateOnly.MinValue && toDate == DateOnly.MaxValue
            ? "كل الفترات"
            : $"من {fromDate:yyyy/MM/dd} إلى {toDate:yyyy/MM/dd}";

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
        foreach (var cell in sheet.CellsUsed().Where(cell => cell.DataType == XLDataType.Number))
        {
            cell.Style.NumberFormat.Format = "0";
        }
        sheet.Columns().AdjustToContents();
        sheet.RangeUsed()?.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        sheet.RangeUsed()?.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
    }

    private static void AddHeader(TableDescriptor table, params string[] headers)
    {
        foreach (var header in headers)
        {
            table.Cell()
                .Background(Colors.Blue.Lighten4)
                .Border(0.7f)
                .BorderColor(Colors.Grey.Lighten1)
                .PaddingVertical(8)
                .PaddingHorizontal(6)
                .AlignCenter()
                .AlignMiddle()
                .Text(header)
                .Bold();
        }
    }

    private static void AddSummaryRow(TableDescriptor table, string label, string value)
    {
        AddBodyCell(table, label, true);
        AddBodyCell(table, value);
    }

    private static void AddBodyCell(TableDescriptor table, string value, bool bold = false)
    {
        var text = table.Cell()
            .Border(0.7f)
            .BorderColor(Colors.Grey.Lighten1)
            .PaddingVertical(7)
            .PaddingHorizontal(6)
            .AlignRight()
            .AlignMiddle()
            .Text(value);

        if (bold)
        {
            text.Bold();
        }
    }
}
