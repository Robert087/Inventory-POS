using AutoPartsPOS.Application.Reports.Dtos;

namespace AutoPartsPOS.Application.Reports.Interfaces;

public interface IReportExportService
{
    Task ExportSalesReportToPdfAsync(SalesReportDto report, string filePath, CancellationToken cancellationToken = default);

    Task ExportSalesReportToExcelAsync(SalesReportDto report, string filePath, CancellationToken cancellationToken = default);

    Task ExportProfitReportToPdfAsync(ProfitReportDto report, string filePath, CancellationToken cancellationToken = default);

    Task ExportProfitReportToExcelAsync(ProfitReportDto report, string filePath, CancellationToken cancellationToken = default);

    Task ExportInventoryReportToPdfAsync(InventoryReportDto report, string filePath, CancellationToken cancellationToken = default);

    Task ExportInventoryReportToExcelAsync(InventoryReportDto report, string filePath, CancellationToken cancellationToken = default);
}
