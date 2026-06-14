using AutoPartsPOS.Application.Reports.Dtos;
using AutoPartsPOS.Application.Reports.Interfaces;

namespace AutoPartsPOS.Application.Reports.Services;

public sealed class ReportingService(IReportingRepository reportingRepository) : IReportingService
{
    public Task<SalesReportDto> GetSalesReportAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default) =>
        reportingRepository.GetSalesReportAsync(fromDate, toDate, cancellationToken);

    public Task<ProfitReportDto> GetProfitReportAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default) =>
        reportingRepository.GetProfitReportAsync(fromDate, toDate, cancellationToken);

    public Task<InventoryReportDto> GetInventoryReportAsync(CancellationToken cancellationToken = default) =>
        reportingRepository.GetInventoryReportAsync(cancellationToken);
}
