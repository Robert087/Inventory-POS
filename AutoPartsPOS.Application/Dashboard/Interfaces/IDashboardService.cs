using AutoPartsPOS.Application.Dashboard.Dtos;

namespace AutoPartsPOS.Application.Dashboard.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> LoadAsync(CancellationToken cancellationToken = default);

    Task<decimal> GetDailySalesAsync(DateOnly date, CancellationToken cancellationToken = default);

    Task<MonthlyDashboardStatisticsDto> GetMonthlyStatisticsAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);
}
