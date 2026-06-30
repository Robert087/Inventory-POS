using AutoPartsPOS.Application.Common.Interfaces;
using AutoPartsPOS.Application.Catalog.Interfaces;
using AutoPartsPOS.Application.Analytics.Interfaces;
using AutoPartsPOS.Application.Inventory.Interfaces;
using AutoPartsPOS.Application.Purchases.Interfaces;
using AutoPartsPOS.Application.Sales.Interfaces;
using AutoPartsPOS.Application.Suppliers.Interfaces;
using AutoPartsPOS.Application.Insights.Interfaces;
using AutoPartsPOS.Application.Reports.Interfaces;
using AutoPartsPOS.Persistence.Catalog;
using AutoPartsPOS.Persistence.Inventory;
using AutoPartsPOS.Persistence.Purchases;
using AutoPartsPOS.Persistence.Repositories;
using AutoPartsPOS.Persistence.Reporting;
using AutoPartsPOS.Persistence.Sales;
using AutoPartsPOS.Persistence.Suppliers;
using AutoPartsPOS.Application.HomeExpenses.Interfaces;
using AutoPartsPOS.Persistence.HomeExpenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace AutoPartsPOS.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dataDirectory);
        Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "Logs"));
        Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "Backups"));
        var databasePath = Path.Combine(dataDirectory, "Database.db");
        var connectionString = $"Data Source={databasePath}";

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite(connectionString, sqlite =>
            {
                sqlite.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            });
        });

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IAppSettingsService, AppSettingsService>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IPurchaseInvoiceRepository, PurchaseInvoiceRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ISalesInvoiceRepository, SalesInvoiceRepository>();
        services.AddScoped<ISalesAnalyticsRepository, SalesAnalyticsRepository>();
        services.AddScoped<IReportingRepository, ReportingRepository>();
        services.AddScoped<IInsightsRepository, InsightsRepository>();
        services.AddScoped<IHomeExpenseRepository, HomeExpenseRepository>();
        services.AddSingleton<DatabaseInitializer>();

        return services;
    }
}
