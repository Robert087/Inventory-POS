using Microsoft.Extensions.DependencyInjection;
using AutoPartsPOS.Application.Catalog.Interfaces;
using AutoPartsPOS.Application.Catalog.Services;
using AutoPartsPOS.Application.Analytics.Interfaces;
using AutoPartsPOS.Application.Analytics.Services;
using AutoPartsPOS.Application.Inventory.Interfaces;
using AutoPartsPOS.Application.Inventory.Services;
using AutoPartsPOS.Application.Purchases.Interfaces;
using AutoPartsPOS.Application.Purchases.Services;
using AutoPartsPOS.Application.Sales.Interfaces;
using AutoPartsPOS.Application.Sales.Services;
using AutoPartsPOS.Application.Settings.Interfaces;
using AutoPartsPOS.Application.Settings.Services;
using AutoPartsPOS.Application.Suppliers.Interfaces;
using AutoPartsPOS.Application.Suppliers.Services;
using AutoPartsPOS.Application.Dashboard.Interfaces;
using AutoPartsPOS.Application.Dashboard.Services;
using AutoPartsPOS.Application.LatestPrices.Interfaces;
using AutoPartsPOS.Application.LatestPrices.Services;
using AutoPartsPOS.Application.Insights.Interfaces;
using AutoPartsPOS.Application.Insights.Services;
using AutoPartsPOS.Application.Reports.Interfaces;
using AutoPartsPOS.Application.Reports.Services;
using AutoPartsPOS.Application.HomeExpenses.Interfaces;
using AutoPartsPOS.Application.HomeExpenses.Services;

namespace AutoPartsPOS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IApplicationSettingsService, ApplicationSettingsService>();
        services.AddScoped<IPurchaseInvoiceService, PurchaseInvoiceService>();
        services.AddScoped<IInventoryLedgerService, InventoryLedgerService>();
        services.AddScoped<ISalesInvoiceService, SalesInvoiceService>();
        services.AddScoped<ISalesAnalyticsService, SalesAnalyticsService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<ISmartInsightsService, SmartInsightsService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ILatestPriceService, LatestPriceService>();
        services.AddScoped<IHomeExpenseService, HomeExpenseService>();

        return services;
    }
}
