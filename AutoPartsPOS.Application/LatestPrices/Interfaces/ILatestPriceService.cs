using AutoPartsPOS.Application.Common.Models;
using AutoPartsPOS.Application.LatestPrices.Dtos;

namespace AutoPartsPOS.Application.LatestPrices.Interfaces;

public interface ILatestPriceService
{
    Task<IReadOnlyList<LatestPriceDto>> SearchAsync(string? searchText, CancellationToken cancellationToken = default);

    Task<OperationResult> SaveAsync(SaveLatestPriceDto dto, CancellationToken cancellationToken = default);
}
