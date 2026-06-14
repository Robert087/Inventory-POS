using AutoPartsPOS.Application.Common.Models;
using AutoPartsPOS.Application.Settings.Dtos;

namespace AutoPartsPOS.Application.Settings.Interfaces;

public interface IApplicationSettingsService
{
    Task<ApplicationSettingsDto> LoadAsync(CancellationToken cancellationToken = default);

    Task<OperationResult> SaveAsync(ApplicationSettingsDto settings, CancellationToken cancellationToken = default);

    Task<ApplicationSettingsDto> ResetToDefaultsAsync(CancellationToken cancellationToken = default);
}
