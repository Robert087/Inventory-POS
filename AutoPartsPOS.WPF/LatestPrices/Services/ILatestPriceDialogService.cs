using AutoPartsPOS.Application.LatestPrices.Dtos;

namespace AutoPartsPOS.WPF.LatestPrices.Services;

public interface ILatestPriceDialogService
{
    Task<bool> ShowLatestPriceDialogAsync(LatestPriceDto? latestPrice);
}
