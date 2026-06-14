namespace AutoPartsPOS.Application.Common.Interfaces;

public interface IAppSettingsService
{
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);

    Task SetValueAsync(string key, string? value, CancellationToken cancellationToken = default);
}
