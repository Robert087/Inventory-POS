using AutoPartsPOS.Application.Settings.Interfaces;
using AutoPartsPOS.Application.Sales.Dtos;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace AutoPartsPOS.WPF.Sales.Services;

public sealed class SalesInvoicePrintService(IApplicationSettingsService settingsService) : ISalesInvoicePrintService
{
    public async Task PreviewAsync(SalesInvoiceDetailsDto invoice, CancellationToken cancellationToken = default)
    {
        var document = await CreatePreviewDocumentAsync(invoice, cancellationToken);
        var previewWindow = new Window
        {
            Title = $"معاينة فاتورة {invoice.InvoiceNumber}",
            Width = 900,
            Height = 700,
            FlowDirection = FlowDirection.RightToLeft,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new FlowDocumentScrollViewer
            {
                Document = document,
                IsToolBarVisible = true
            }
        };

        previewWindow.ShowDialog();
    }

    public async Task PrintAsync(SalesInvoiceDetailsDto invoice, CancellationToken cancellationToken = default)
    {
        var document = await CreatePreviewDocumentAsync(invoice, cancellationToken);
        var printDialog = new PrintDialog();

        if (printDialog.ShowDialog() == true)
        {
            printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"فاتورة بيع {invoice.InvoiceNumber}");
        }
    }

    public async Task<FlowDocument> CreatePreviewDocumentAsync(SalesInvoiceDetailsDto invoice, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.LoadAsync(cancellationToken);
        var document = new FlowDocument
        {
            FlowDirection = FlowDirection.RightToLeft,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 13,
            PagePadding = new Thickness(40),
            ColumnWidth = double.PositiveInfinity
        };

        document.Blocks.Add(new Paragraph(new Run(settings.StoreName))
        {
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center
        });

        document.Blocks.Add(new Paragraph(new Run($"{settings.StorePhone}   {settings.StoreAddress}"))
        {
            TextAlignment = TextAlignment.Center
        });

        document.Blocks.Add(new Paragraph(new Run($"فاتورة بيع رقم: {invoice.InvoiceNumber}"))
        {
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 18, 0, 8)
        });

        document.Blocks.Add(new Paragraph(new Run($"التاريخ: {invoice.InvoiceDate}")));

        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(0.5),
            Margin = new Thickness(0, 16, 0, 16)
        };

        table.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

        var rowGroup = new TableRowGroup();
        table.RowGroups.Add(rowGroup);

        AddRow(rowGroup, "المنتج", "الكمية", "السعر", "الإجمالي", true);

        foreach (var item in invoice.Items)
        {
            AddRow(
                rowGroup,
                item.ProductNameAr,
                item.Quantity.ToString("N3"),
                $"{item.UnitPrice:N2} {settings.CurrencySymbol}",
                $"{item.TotalPrice:N2} {settings.CurrencySymbol}",
                false);
        }

        document.Blocks.Add(table);
        document.Blocks.Add(new Paragraph(new Run($"الإجمالي قبل الخصم: {invoice.SubtotalAmount:N2} {settings.CurrencySymbol}")) { TextAlignment = TextAlignment.Left });
        document.Blocks.Add(new Paragraph(new Run($"الخصم: {invoice.DiscountAmount:N2} {settings.CurrencySymbol}")) { TextAlignment = TextAlignment.Left });
        document.Blocks.Add(new Paragraph(new Run($"الصافي: {invoice.NetTotalAmount:N2} {settings.CurrencySymbol}"))
        {
            FontWeight = FontWeights.Bold,
            FontSize = 16,
            TextAlignment = TextAlignment.Left
        });

        if (!string.IsNullOrWhiteSpace(invoice.Notes))
        {
            document.Blocks.Add(new Paragraph(new Run($"ملاحظات: {invoice.Notes}")));
        }

        return document;
    }

    private static void AddRow(TableRowGroup rowGroup, string first, string second, string third, string fourth, bool isHeader)
    {
        var row = new TableRow();
        rowGroup.Rows.Add(row);

        row.Cells.Add(CreateCell(first, isHeader));
        row.Cells.Add(CreateCell(second, isHeader));
        row.Cells.Add(CreateCell(third, isHeader));
        row.Cells.Add(CreateCell(fourth, isHeader));
    }

    private static TableCell CreateCell(string text, bool isHeader)
    {
        return new TableCell(new Paragraph(new Run(text)))
        {
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(0.5),
            Padding = new Thickness(6),
            FontWeight = isHeader ? FontWeights.Bold : FontWeights.Normal
        };
    }
}
