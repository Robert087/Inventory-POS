using AutoPartsPOS.Application;
using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.Application.Catalog.Interfaces;
using AutoPartsPOS.Application.Dashboard.Interfaces;
using AutoPartsPOS.Application.Inventory.Interfaces;
using AutoPartsPOS.Application.Purchases.Dtos;
using AutoPartsPOS.Application.Purchases.Interfaces;
using AutoPartsPOS.Application.Reports.Interfaces;
using AutoPartsPOS.Application.Sales.Dtos;
using AutoPartsPOS.Application.Sales.Interfaces;
using AutoPartsPOS.Application.Suppliers.Dtos;
using AutoPartsPOS.Application.Suppliers.Interfaces;
using AutoPartsPOS.Domain.Inventory;
using AutoPartsPOS.Infrastructure;
using AutoPartsPOS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace AutoPartsPOS.RuntimeValidation;

internal static class SqliteIntegrationValidation
{
    public static async Task RunAsync()
    {
        var validationDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(validationDirectory);
        var databasePath = Path.Combine(validationDirectory, "Database.db");
        File.Delete(databasePath);

        var configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure();
        services.AddPersistence(configuration);

        await using var provider = services.BuildServiceProvider();
        await provider.GetRequiredService<DatabaseInitializer>().InitializeAsync();

        Assert(File.Exists(databasePath), "SQLite database was not created.");

        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert(!(await dbContext.Database.GetPendingMigrationsAsync()).Any(), "SQLite migrations are pending.");
        Assert(await dbContext.AppSettings.CountAsync() >= 7, "Required settings were not seeded.");

        var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
        var supplierService = scope.ServiceProvider.GetRequiredService<ISupplierService>();
        var purchaseService = scope.ServiceProvider.GetRequiredService<IPurchaseInvoiceService>();
        var salesService = scope.ServiceProvider.GetRequiredService<ISalesInvoiceService>();
        var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryLedgerService>();
        var reportingService = scope.ServiceProvider.GetRequiredService<IReportingService>();
        var dashboardService = scope.ServiceProvider.GetRequiredService<IDashboardService>();

        Assert((await categoryService.SaveAsync(new SaveCategoryDto { NameAr = "تصنيف اختبار SQLite" })).Succeeded, "Category creation failed.");
        var category = (await categoryService.SearchAsync("SQLite")).Single();

        Assert((await productService.SaveAsync(new SaveProductDto
        {
            ProductCode = "SQLITE-001",
            Barcode = "SQLITE-BARCODE-001",
            NameAr = "منتج اختبار SQLite",
            CategoryId = category.Id,
            PurchasePrice = 10,
            SellingPrice = 20,
            CurrentStock = 0,
            MinimumStock = 2
        })).Succeeded, "Product creation failed.");
        var product = (await productService.SearchAsync("SQLITE-001", category.Id)).Single();

        Assert((await supplierService.SaveAsync(new SaveSupplierDto { NameAr = "مورد اختبار SQLite" })).Succeeded, "Supplier creation failed.");
        var supplier = (await supplierService.SearchAsync("SQLite")).Single();

        Assert((await purchaseService.CreateAsync(new CreatePurchaseInvoiceDto
        {
            InvoiceNumber = "PUR-SQLITE-001",
            SupplierId = supplier.Id,
            InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
            Items = [new CreatePurchaseInvoiceItemDto { ProductId = product.Id, Quantity = 10, UnitPrice = 10 }]
        })).Succeeded, "Purchase invoice creation failed.");
        var productAfterPurchase = (await productService.GetByIdAsync(product.Id))!;
        Assert(productAfterPurchase.CurrentStock == 10, "Purchase did not increase stock.");
        Assert(productAfterPurchase.CurrentAverageCost == 10, "Purchase did not set weighted average cost.");

        Assert((await salesService.CreateAsync(new CreateSalesInvoiceDto
        {
            InvoiceNumber = "SAL-SQLITE-001",
            InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
            DiscountAmount = 5,
            Items = [new CreateSalesInvoiceItemDto { ProductId = product.Id, Quantity = 4, UnitPrice = 20 }]
        })).Succeeded, "Sales invoice creation failed.");
        Assert((await productService.GetByIdAsync(product.Id))!.CurrentStock == 6, "Sale did not decrease stock.");

        var sale = (await salesService.SearchAsync("SAL-SQLITE-001")).Single();
        var saleDetails = await salesService.GetDetailsAsync(sale.Id);
        Assert(saleDetails!.Items.Single().UnitCost == 10, "Sale did not store WAC unit cost snapshot.");
        Assert(saleDetails.Items.Single().TotalCost == 40, "Sale did not store WAC total cost snapshot.");
        Assert((await salesService.VoidAsync(sale.Id, "اختبار SQLite")).Succeeded, "Sales void failed.");
        Assert((await productService.GetByIdAsync(product.Id))!.CurrentStock == 10, "Sales void did not restore stock.");

        var ledger = await inventoryService.SearchAsync(null, product.Id);
        Assert(ledger.Count == 3, "Expected purchase, sale, and void-sale ledger entries.");

        var today = DateOnly.FromDateTime(DateTime.Today);
        _ = await reportingService.GetSalesReportAsync(today.AddDays(-1), today.AddDays(1));
        _ = await reportingService.GetProfitReportAsync(today.AddDays(-1), today.AddDays(1));
        _ = await reportingService.GetInventoryReportAsync();
        _ = await dashboardService.LoadAsync();

        Console.WriteLine($"SQLITE_DATABASE_PATH:{databasePath}");
        Console.WriteLine("SQLITE_AUTO_CREATE:PASS");
        Console.WriteLine("SQLITE_MIGRATIONS:PASS");
        Console.WriteLine("SQLITE_SETTINGS_SEED:PASS");
        Console.WriteLine("SQLITE_PURCHASE_TRANSACTION:PASS");
        Console.WriteLine("SQLITE_SALES_TRANSACTION:PASS");
        Console.WriteLine("SQLITE_VOID_SALE_TRANSACTION:PASS");
        Console.WriteLine("SQLITE_INVENTORY_LEDGER:PASS");
        Console.WriteLine("SQLITE_REPORTS:PASS");
        Console.WriteLine("SQLITE_DASHBOARD:PASS");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
