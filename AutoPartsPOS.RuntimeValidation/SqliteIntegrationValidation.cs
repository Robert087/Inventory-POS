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
using AutoPartsPOS.Domain.Common;
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

        Assert((await categoryService.SaveAsync(new SaveCategoryDto { NameAr = "تصنيف قابل للحذف" })).Succeeded, "Disposable category creation failed.");
        var disposableCategory = (await categoryService.SearchAsync("قابل للحذف")).Single();
        Assert((await categoryService.DeleteAsync(disposableCategory.Id)).Succeeded, "Unlinked category deletion failed.");
        Assert(!(await categoryService.SearchAsync("قابل للحذف")).Any(), "Deleted category is still present.");

        Assert((await productService.SaveAsync(new SaveProductDto
        {
            ProductCode = "SQLITE-001",
            Barcode = "SQLITE-BARCODE-001",
            NameAr = "منتج اختبار SQLite",
            CategoryId = category.Id,
            PurchasePrice = 10,
            SellingPrice = 0,
            CurrentStock = 0,
            MinimumStock = 2
        })).Succeeded, "Product creation failed.");
        var product = (await productService.SearchAsync("SQLITE-001", category.Id)).Single();
        Assert(product.SellingPrice == 0, "Product without selling price was not saved safely.");

        Assert((await productService.SaveAsync(new SaveProductDto
        {
            ProductCode = "SQLITE-DELETE",
            NameAr = "صنف قابل للحذف",
            CategoryId = category.Id,
            PurchasePrice = 1,
            SellingPrice = 2
        })).Succeeded, "Disposable product creation failed.");
        var disposableProduct = (await productService.SearchAsync("SQLITE-DELETE", category.Id)).Single();
        Assert((await productService.DeleteAsync(disposableProduct.Id)).Succeeded, "Unlinked product deletion failed.");
        Assert(!(await productService.SearchAsync("SQLITE-DELETE", category.Id)).Any(), "Deleted product is still present.");

        Assert((await supplierService.SaveAsync(new SaveSupplierDto { NameAr = "مورد اختبار SQLite" })).Succeeded, "Supplier creation failed.");
        var supplier = (await supplierService.SearchAsync("SQLite")).Single();

        Assert((await supplierService.SaveAsync(new SaveSupplierDto { NameAr = "مورد قابل للحذف" })).Succeeded, "Disposable supplier creation failed.");
        var disposableSupplier = (await supplierService.SearchAsync("قابل للحذف")).Single();
        Assert((await supplierService.DeleteAsync(disposableSupplier.Id)).Succeeded, "Unlinked supplier deletion failed.");
        Assert(!(await supplierService.SearchAsync("قابل للحذف")).Any(), "Deleted supplier is still present.");

        Assert((await productService.SaveAsync(new SaveProductDto
        {
            ProductCode = "SQLITE-WAC",
            NameAr = "منتج اختبار WAC",
            CategoryId = category.Id,
            PurchasePrice = 100,
            SellingPrice = 200,
            CurrentStock = 0,
            MinimumStock = 2
        })).Succeeded, "WAC product creation failed.");
        var wacProduct = (await productService.SearchAsync("SQLITE-WAC", category.Id)).Single();

        Assert((await purchaseService.CreateAsync(new CreatePurchaseInvoiceDto
        {
            InvoiceNumber = "PUR-SQLITE-WAC-001",
            SupplierId = supplier.Id,
            InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
            Items = [new CreatePurchaseInvoiceItemDto { ProductId = wacProduct.Id, Quantity = 10, UnitPrice = 100 }]
        })).Succeeded, "First WAC purchase failed.");
        Assert((await purchaseService.CreateAsync(new CreatePurchaseInvoiceDto
        {
            InvoiceNumber = "PUR-SQLITE-WAC-002",
            SupplierId = supplier.Id,
            InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
            Items = [new CreatePurchaseInvoiceItemDto { ProductId = wacProduct.Id, Quantity = 5, UnitPrice = 160 }]
        })).Succeeded, "WAC replenishment purchase failed.");
        var wacProductAfterReplenishment = (await productService.GetByIdAsync(wacProduct.Id))!;
        Assert(wacProductAfterReplenishment.CurrentStock == 15, "WAC replenishment did not increase stock to 15.");
        Assert(wacProductAfterReplenishment.CurrentAverageCost == 120, "WAC replenishment average cost is incorrect.");
        Assert(wacProductAfterReplenishment.PurchasePrice == 160, "Latest purchase price was not stored separately from average cost.");

        Assert((await salesService.CreateAsync(new CreateSalesInvoiceDto
        {
            InvoiceNumber = "SAL-SQLITE-WAC",
            InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
            PaymentStatus = InvoicePaymentStatus.Paid,
            Items = [new CreateSalesInvoiceItemDto { ProductId = wacProduct.Id, Quantity = 2, UnitPrice = 200 }]
        })).Succeeded, "WAC product sale failed.");
        var wacSaleDetails = await salesService.GetDetailsAsync((await salesService.SearchAsync("SAL-SQLITE-WAC")).Single().Id);
        Assert(wacSaleDetails!.Items.Single().UnitCost == 120, "Sale after replenishment did not use weighted average cost.");
        Assert(wacSaleDetails.Items.Single().TotalCost == 240, "Sale total cost after replenishment is incorrect.");

        var wacInventoryReport = await reportingService.GetInventoryReportAsync();
        var wacInventoryItem = wacInventoryReport.Items.Single(item => item.ProductId == wacProduct.Id);
        Assert(wacInventoryItem.CurrentStock == 13, "WAC product stock after sale is incorrect.");
        Assert(wacInventoryItem.CurrentAverageCost == 120, "WAC product average cost changed after sale.");
        Assert(wacInventoryItem.InventoryValue == 13 * 120, "Inventory value must use weighted average cost.");

        Assert(!(await purchaseService.CreateAsync(new CreatePurchaseInvoiceDto
        {
            InvoiceNumber = "PUR-SQLITE-ZERO",
            SupplierId = supplier.Id,
            InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
            Items = [new CreatePurchaseInvoiceItemDto { ProductId = wacProduct.Id, Quantity = 1, UnitPrice = 0 }]
        })).Succeeded, "Zero purchase price was accepted.");

        Assert((await purchaseService.CreateAsync(new CreatePurchaseInvoiceDto
        {
            InvoiceNumber = "PUR-SQLITE-001",
            SupplierId = supplier.Id,
            InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
            Items = [new CreatePurchaseInvoiceItemDto { ProductId = product.Id, Quantity = 10, UnitPrice = 10 }]
        })).Succeeded, "Purchase invoice creation failed.");
        Assert(!(await productService.DeleteAsync(product.Id)).Succeeded, "Linked product deletion was allowed.");
        Assert(!(await categoryService.DeleteAsync(category.Id)).Succeeded, "Category containing products was deleted.");
        Assert(!(await supplierService.DeleteAsync(supplier.Id)).Succeeded, "Supplier with purchase invoices was deleted.");
        Console.WriteLine("SQLITE_SAFE_DELETE_RULES:PASS");
        var productAfterPurchase = (await productService.GetByIdAsync(product.Id))!;
        Assert(productAfterPurchase.CurrentStock == 10, "Purchase did not increase stock.");
        Assert(productAfterPurchase.CurrentAverageCost == 10, "Purchase did not set weighted average cost.");

        Assert((await salesService.CreateAsync(new CreateSalesInvoiceDto
        {
            InvoiceNumber = "SAL-SQLITE-001",
            InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
            DiscountAmount = 5,
            PaymentStatus = InvoicePaymentStatus.Paid,
            Items = [new CreateSalesInvoiceItemDto { ProductId = product.Id, Quantity = 4, UnitPrice = 20 }]
        })).Succeeded, "Sales invoice creation failed.");
        Assert((await productService.GetByIdAsync(product.Id))!.CurrentStock == 6, "Sale did not decrease stock.");

        var sale = (await salesService.SearchAsync("SAL-SQLITE-001")).Single();
        var saleDetails = await salesService.GetDetailsAsync(sale.Id);
        Assert(sale.Status == "مدفوعة بالكامل" && sale.PaidAmount == 75 && sale.RemainingAmount == 0,
            "Fully paid sales invoice payment values are incorrect.");
        Assert(saleDetails!.Items.Single().UnitCost == 10, "Sale did not store WAC unit cost snapshot.");
        Assert(saleDetails.Items.Single().TotalCost == 40, "Sale did not store WAC total cost snapshot.");
        Assert((await salesService.VoidAsync(sale.Id, "اختبار SQLite")).Succeeded, "Sales void failed.");
        Assert((await productService.GetByIdAsync(product.Id))!.CurrentStock == 10, "Sales void did not restore stock.");

        Assert((await salesService.CreateAsync(new CreateSalesInvoiceDto
        {
            InvoiceNumber = "SAL-SQLITE-PARTIAL",
            InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
            PaymentStatus = InvoicePaymentStatus.PartiallyPaid,
            PaidAmount = 5,
            Items = [new CreateSalesInvoiceItemDto { ProductId = product.Id, Quantity = 1, UnitPrice = 20 }]
        })).Succeeded, "Partially paid sales invoice creation failed.");
        var partialSale = (await salesService.SearchAsync("SAL-SQLITE-PARTIAL")).Single();
        Assert(partialSale.PaidAmount == 5 && partialSale.RemainingAmount == 15 && partialSale.Status == "مدفوعة جزئياً",
            "Partial payment calculation is incorrect.");
        var ledgerCountBeforePaymentEdit = (await inventoryService.SearchAsync(null, product.Id)).Count;
        Assert((await salesService.UpdatePaymentAsync(partialSale.Id, InvoicePaymentStatus.Paid, 0)).Succeeded,
            "Payment-only edit failed.");
        var editedPartialSale = (await salesService.SearchAsync("SAL-SQLITE-PARTIAL")).Single();
        Assert(editedPartialSale.PaidAmount == 20 && editedPartialSale.RemainingAmount == 0 && editedPartialSale.Status == "مدفوعة بالكامل",
            "Payment-only edit values are incorrect.");
        Assert((await inventoryService.SearchAsync(null, product.Id)).Count == ledgerCountBeforePaymentEdit,
            "Payment-only edit created a duplicate stock movement.");

        Assert((await salesService.CreateAsync(new CreateSalesInvoiceDto
        {
            InvoiceNumber = "SAL-SQLITE-UNPAID",
            InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
            PaymentStatus = InvoicePaymentStatus.Unpaid,
            PaidAmount = 0,
            Items = [new CreateSalesInvoiceItemDto { ProductId = product.Id, Quantity = 1, UnitPrice = 20 }]
        })).Succeeded, "Unpaid sales invoice creation failed.");
        var unpaidSale = (await salesService.SearchAsync("SAL-SQLITE-UNPAID")).Single();
        Assert(unpaidSale.PaidAmount == 0 && unpaidSale.RemainingAmount == 20 && unpaidSale.Status == "غير مدفوعة",
            "Unpaid invoice payment values are incorrect.");

        var salesFilterDate = DateOnly.FromDateTime(DateTime.Today);
        var salesToday = await salesService.SearchAsync(null, salesFilterDate, salesFilterDate);
        Assert(salesToday.Count == 4, "Same-day sales filtering excluded invoices from the selected date.");
        var futureSales = await salesService.SearchAsync(null, salesFilterDate.AddDays(1), salesFilterDate.AddDays(1));
        Assert(futureSales.Count == 0, "Sales date filtering returned invoices outside the selected date.");

        var ledger = await inventoryService.SearchAsync(null, product.Id);
        Assert(ledger.Count == 5, "Expected purchase, sale, void-sale, partial-sale, and unpaid-sale ledger entries.");
        var todayOnlyLedger = await inventoryService.SearchAsync(null, product.Id, DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today));
        Assert(todayOnlyLedger.Count == ledger.Count, "Inventory date filtering excluded today's movements.");
        var futureLedger = await inventoryService.SearchAsync(null, product.Id, DateOnly.FromDateTime(DateTime.Today.AddDays(1)), null);
        Assert(futureLedger.Count == 0, "Inventory date filtering returned movements outside the range.");

        var today = DateOnly.FromDateTime(DateTime.Today);
        _ = await reportingService.GetSalesReportAsync(today.AddDays(-1), today.AddDays(1));
        var profitReport = await reportingService.GetProfitReportAsync(today, today);
        _ = await reportingService.GetInventoryReportAsync();
        var dailySales = await dashboardService.GetDailySalesAsync(today);
        var dailyReport = await reportingService.GetSalesReportAsync(today, today);
        Assert(dailySales == dailyReport.NetSales, "Dashboard daily sales do not match the exact-date sales report.");
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var monthProfitReport = await reportingService.GetProfitReportAsync(monthStart, monthEnd);
        var monthStatistics = await dashboardService.GetMonthlyStatisticsAsync(today.Year, today.Month);
        Assert(monthStatistics.NetProfit == monthProfitReport.Profit, "Dashboard monthly profit does not match the profit report.");

        Console.WriteLine($"SQLITE_DATABASE_PATH:{databasePath}");
        Console.WriteLine("SQLITE_AUTO_CREATE:PASS");
        Console.WriteLine("SQLITE_MIGRATIONS:PASS");
        Console.WriteLine("SQLITE_SETTINGS_SEED:PASS");
        Console.WriteLine("SQLITE_WAC_REPLENISHMENT:PASS");
        Console.WriteLine("SQLITE_ZERO_PURCHASE_PRICE_REJECTED:PASS");
        Console.WriteLine("SQLITE_PURCHASE_TRANSACTION:PASS");
        Console.WriteLine("SQLITE_SALES_TRANSACTION:PASS");
        Console.WriteLine("SQLITE_VOID_SALE_TRANSACTION:PASS");
        Console.WriteLine("SQLITE_INVENTORY_LEDGER:PASS");
        Console.WriteLine("SQLITE_INVENTORY_DATE_FILTER:PASS");
        Console.WriteLine("SQLITE_SALES_PAYMENT_TRACKING:PASS");
        Console.WriteLine("SQLITE_OPTIONAL_SELLING_PRICE:PASS");
        Console.WriteLine("SQLITE_REPORTS:PASS");
        Console.WriteLine("SQLITE_DASHBOARD_PROFIT_FILTER:PASS");
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
