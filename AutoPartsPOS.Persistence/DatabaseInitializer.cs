using AutoPartsPOS.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoPartsPOS.Persistence;

public sealed class DatabaseInitializer(
    IServiceScopeFactory scopeFactory,
    ILogger<DatabaseInitializer> logger)
{
    private const string InitialMigration = "20260613213419_InitialSqliteSchema";

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

        if (dbContext.Database.IsSqlite() && pendingMigrations.Length > 0)
        {
            if (pendingMigrations.Contains(InitialMigration, StringComparer.Ordinal))
            {
                await dbContext.Database.MigrateAsync(InitialMigration, cancellationToken);
                pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
            }

            await ApplySqliteFallbackMigrationsAsync(dbContext, pendingMigrations, cancellationToken);
        }
        else
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }

        var seededSettings = await SeedSettingsAsync(dbContext, cancellationToken);

        logger.LogInformation(
            "SQLite initialization completed. AppliedMigrations: {AppliedMigrationCount}; SeededSettings: {SeededSettings}; Database: {DatabasePath}",
            pendingMigrations.Length,
            seededSettings,
            databasePath);
    }

    private static async Task ApplySqliteFallbackMigrationsAsync(
        AppDbContext dbContext,
        IReadOnlyCollection<string> pendingMigrations,
        CancellationToken cancellationToken)
    {
        foreach (var migration in pendingMigrations)
        {
            switch (migration)
            {
                case "20260615035130_AddInvoicePaymentStatus":
                    await EnsureColumnAsync(dbContext, "sales_invoices", "payment_status", "ALTER TABLE \"sales_invoices\" ADD \"payment_status\" TEXT NOT NULL DEFAULT 'Unpaid';", cancellationToken);
                    await EnsureColumnAsync(dbContext, "purchase_invoices", "payment_status", "ALTER TABLE \"purchase_invoices\" ADD \"payment_status\" TEXT NOT NULL DEFAULT 'Unpaid';", cancellationToken);
                    break;

                case "20260618204723_AddWeightedAverageCost":
                    await EnsureColumnAsync(dbContext, "sales_invoice_items", "total_cost", "ALTER TABLE \"sales_invoice_items\" ADD \"total_cost\" TEXT NOT NULL DEFAULT '0.0';", cancellationToken);
                    await EnsureColumnAsync(dbContext, "sales_invoice_items", "unit_cost", "ALTER TABLE \"sales_invoice_items\" ADD \"unit_cost\" TEXT NOT NULL DEFAULT '0.0';", cancellationToken);
                    await EnsureColumnAsync(dbContext, "products", "current_average_cost", "ALTER TABLE \"products\" ADD \"current_average_cost\" TEXT NOT NULL DEFAULT '0.0';", cancellationToken);
                    await dbContext.Database.ExecuteSqlRawAsync("UPDATE products SET current_average_cost = purchase_price;", cancellationToken);
                    await dbContext.Database.ExecuteSqlRawAsync(
                        """
                        UPDATE sales_invoice_items
                        SET unit_cost = COALESCE((SELECT current_average_cost FROM products WHERE products.Id = sales_invoice_items.product_id), 0),
                            total_cost = ROUND(quantity * COALESCE((SELECT current_average_cost FROM products WHERE products.Id = sales_invoice_items.product_id), 0), 2);
                        """,
                        cancellationToken);
                    break;

                case "20260628001018_AddSalesPaymentTracking":
                    await EnsureColumnAsync(dbContext, "sales_invoices", "paid_amount", "ALTER TABLE \"sales_invoices\" ADD \"paid_amount\" TEXT NOT NULL DEFAULT '0.0';", cancellationToken);
                    await EnsureColumnAsync(dbContext, "sales_invoices", "remaining_amount", "ALTER TABLE \"sales_invoices\" ADD \"remaining_amount\" TEXT NOT NULL DEFAULT '0.0';", cancellationToken);
                    await dbContext.Database.ExecuteSqlRawAsync("UPDATE sales_invoices SET paid_amount = 0, remaining_amount = net_total_amount;", cancellationToken);
                    break;

                case "20260630055252_AddHomeExpenses":
                    if (!await TableExistsAsync(dbContext, "home_expense_days", cancellationToken))
                    {
                        await dbContext.Database.ExecuteSqlRawAsync(
                            """
                            CREATE TABLE "home_expense_days" (
                                "Id" INTEGER NOT NULL CONSTRAINT "PK_home_expense_days" PRIMARY KEY AUTOINCREMENT,
                                "expense_date" TEXT NOT NULL,
                                "total_amount" TEXT NOT NULL,
                                "created_at" TEXT NOT NULL,
                                "created_by" TEXT NULL,
                                "updated_at" TEXT NULL,
                                "updated_by" TEXT NULL,
                                CONSTRAINT "ck_home_expense_days_total_amount_non_negative" CHECK (total_amount >= 0)
                            );
                            """,
                            cancellationToken);
                    }

                    if (!await TableExistsAsync(dbContext, "home_expense_items", cancellationToken))
                    {
                        await dbContext.Database.ExecuteSqlRawAsync(
                            """
                            CREATE TABLE "home_expense_items" (
                                "Id" INTEGER NOT NULL CONSTRAINT "PK_home_expense_items" PRIMARY KEY AUTOINCREMENT,
                                "home_expense_day_id" INTEGER NOT NULL,
                                "note" TEXT NOT NULL,
                                "amount" TEXT NOT NULL,
                                "created_at" TEXT NOT NULL,
                                "created_by" TEXT NULL,
                                "updated_at" TEXT NULL,
                                "updated_by" TEXT NULL,
                                CONSTRAINT "ck_home_expense_items_amount_positive" CHECK (amount > 0),
                                CONSTRAINT "FK_home_expense_items_home_expense_days_home_expense_day_id" FOREIGN KEY ("home_expense_day_id") REFERENCES "home_expense_days" ("Id") ON DELETE CASCADE
                            );
                            """,
                            cancellationToken);
                    }

                    await dbContext.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS \"ux_home_expense_days_expense_date\" ON \"home_expense_days\" (\"expense_date\");", cancellationToken);
                    await dbContext.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"ix_home_expense_items_home_expense_day_id\" ON \"home_expense_items\" (\"home_expense_day_id\");", cancellationToken);
                    break;
            }

            await EnsureMigrationHistoryAsync(dbContext, migration, cancellationToken);
        }
    }

    private static async Task EnsureColumnAsync(AppDbContext dbContext, string tableName, string columnName, string sql, CancellationToken cancellationToken)
    {
        if (await ColumnExistsAsync(dbContext, tableName, columnName, cancellationToken))
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private static async Task<bool> ColumnExistsAsync(AppDbContext dbContext, string tableName, string columnName, CancellationToken cancellationToken)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            await command.Connection!.OpenAsync(cancellationToken);
        }

        command.CommandText = $"PRAGMA table_info(\"{tableName}\");";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<bool> TableExistsAsync(AppDbContext dbContext, string tableName, CancellationToken cancellationToken)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            await command.Connection!.OpenAsync(cancellationToken);
        }

        command.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = $name;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static async Task EnsureMigrationHistoryAsync(AppDbContext dbContext, string migrationId, CancellationToken cancellationToken)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            await command.Connection!.OpenAsync(cancellationToken);
        }

        command.CommandText = "SELECT COUNT(1) FROM __EFMigrationsHistory WHERE MigrationId = $migrationId;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$migrationId";
        parameter.Value = migrationId;
        command.Parameters.Add(parameter);

        var exists = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
        if (!exists)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $@"INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"") VALUES ({migrationId}, '9.0.10');",
                cancellationToken);
        }
    }

    private static async Task<int> SeedSettingsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var defaults = new Dictionary<string, string?>
        {
            ["store_name"] = "Taison System",
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
