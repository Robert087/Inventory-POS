using AutoPartsPOS.Application.Common.Interfaces;
using AutoPartsPOS.Application.Reports.Interfaces;
using AutoPartsPOS.WPF.Catalog.Services;
using AutoPartsPOS.WPF.Catalog.ViewModels;
using AutoPartsPOS.WPF.Inventory.ViewModels;
using AutoPartsPOS.WPF.Purchases.Services;
using AutoPartsPOS.WPF.Purchases.ViewModels;
using AutoPartsPOS.WPF.Reports.Services;
using AutoPartsPOS.WPF.Reports.ViewModels;
using AutoPartsPOS.WPF.Sales.Services;
using AutoPartsPOS.WPF.Sales.ViewModels;
using AutoPartsPOS.WPF.Settings.ViewModels;
using AutoPartsPOS.WPF.Services;
using AutoPartsPOS.WPF.Suppliers.Services;
using AutoPartsPOS.WPF.Suppliers.ViewModels;
using AutoPartsPOS.WPF.LatestPrices.Services;
using AutoPartsPOS.WPF.LatestPrices.ViewModels;
using AutoPartsPOS.WPF.HomeExpenses.Services;
using AutoPartsPOS.WPF.HomeExpenses.ViewModels;
using AutoPartsPOS.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AutoPartsPOS.WPF;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();

        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddScoped<ICatalogDialogService, CatalogDialogService>();
        services.AddScoped<ISupplierDialogService, SupplierDialogService>();
        services.AddScoped<ILatestPriceDialogService, LatestPriceDialogService>();
        services.AddScoped<IHomeExpenseDialogService, HomeExpenseDialogService>();
        services.AddScoped<IPurchaseDialogService, PurchaseDialogService>();
        services.AddScoped<ISalesDialogService, SalesDialogService>();
        services.AddScoped<ISalesInvoicePrintService, SalesInvoicePrintService>();
        services.AddSingleton<IReportExportService, ReportExportService>();

        services.AddSingleton<ShellViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<CategoriesViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<CategoryDialogViewModel>();
        services.AddTransient<ProductDialogViewModel>();
        services.AddTransient<StockReplenishmentDialogViewModel>();
        services.AddTransient<SuppliersViewModel>();
        services.AddTransient<SupplierDialogViewModel>();
        services.AddTransient<LatestPricesViewModel>();
        services.AddTransient<LatestPriceDialogViewModel>();
        services.AddTransient<HomeExpensesViewModel>();
        services.AddTransient<HomeExpenseDialogViewModel>();
        services.AddTransient<HomeExpenseDetailsDialogViewModel>();
        services.AddTransient<AppSettingsViewModel>();
        services.AddTransient<PurchaseInvoicesViewModel>();
        services.AddTransient<PurchaseInvoiceDialogViewModel>();
        services.AddTransient<PurchaseInvoiceDetailsViewModel>();
        services.AddTransient<InventoryLedgerViewModel>();
        services.AddTransient<SalesInvoicesViewModel>();
        services.AddTransient<SalesInvoiceDialogViewModel>();
        services.AddTransient<SalesInvoiceDetailsViewModel>();
        services.AddTransient<ReportsViewModel>();

        return services;
    }
}
