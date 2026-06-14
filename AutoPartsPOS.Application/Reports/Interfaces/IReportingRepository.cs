using AutoPartsPOS.Application.Reports.Dtos;

namespace AutoPartsPOS.Application.Reports.Interfaces;

public interface IReportingRepository
{
    Task<SalesReportDto> GetSalesReportAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);

    Task<ProfitReportDto> GetProfitReportAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);

    Task<InventoryReportDto> GetInventoryReportAsync(CancellationToken cancellationToken = default);
}
