using AutoPartsPOS.Application.Common.Interfaces;
using AutoPartsPOS.Domain.Settings;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsPOS.Persistence.Repositories;

public sealed class AppSettingsService(AppDbContext dbContext) : IAppSettingsService
{
    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        return await dbContext.AppSettings
            .Where(setting => setting.Key == key)
            .Select(setting => setting.Value)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task SetValueAsync(string key, string? value, CancellationToken cancellationToken = default)
    {
        var setting = await dbContext.AppSettings
            .SingleOrDefaultAsync(item => item.Key == key, cancellationToken);

        if (setting is null)
        {
            setting = new AppSetting
            {
                Key = key,
                Value = value
            };

            await dbContext.AppSettings.AddAsync(setting, cancellationToken);
        }
        else
        {
            setting.Value = value;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
