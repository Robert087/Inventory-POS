using AutoPartsPOS.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoPartsPOS.Persistence;

public sealed class DatabaseInitializer(
    IServiceScopeFactory scopeFactory,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var databasePath = dbContext.Database.GetDbConnection().DataSource;
        var databaseExists = File.Exists(databasePath);
        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();

        logger.LogInformation(
            "Starting SQLite initialization. DatabaseExists: {DatabaseExists}; PendingMigrations: {PendingMigrationCount}; Database: {DatabasePath}",
            databaseExists,
            pendingMigrations.Length,
            databasePath);

        await dbContext.Database.MigrateAsync(cancellationToken);
        var seededSettings = await SeedSettingsAsync(dbContext, cancellationToken);

        logger.LogInformation(
            "SQLite initialization completed. AppliedMigrations: {AppliedMigrationCount}; SeededSettings: {SeededSettings}; Database: {DatabasePath}",
            pendingMigrations.Length,
            seededSettings,
            databasePath);
    }

    private static async Task<int> SeedSettingsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var defaults = new Dictionary<string, string?>
        {
            ["store_name"] = "محل إكسسوارات السيارات",
            ["store_phone"] = string.Empty,
            ["store_address"] = string.Empty,
            ["currency_symbol"] = "ج.م",
            ["low_stock_alert_enabled"] = bool.TrueString,
            ["slow_moving_days"] = "90",
            ["top_selling_days"] = "30"
        };

        var existingKeys = await dbContext.AppSettings
            .Select(setting => setting.Key)
            .ToListAsync(cancellationToken);
        var missingSettings = defaults
            .Where(setting => !existingKeys.Contains(setting.Key))
            .ToArray();

        foreach (var setting in missingSettings)
        {
            dbContext.AppSettings.Add(new AppSetting
            {
                Key = setting.Key,
                Value = setting.Value,
                Description = "إعداد افتراضي للنظام",
                IsSystem = true
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return missingSettings.Length;
    }
}
