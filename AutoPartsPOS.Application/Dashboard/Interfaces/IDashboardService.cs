using AutoPartsPOS.Application.Dashboard.Dtos;

namespace AutoPartsPOS.Application.Dashboard.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> LoadAsync(CancellationToken cancellationToken = default);
}
