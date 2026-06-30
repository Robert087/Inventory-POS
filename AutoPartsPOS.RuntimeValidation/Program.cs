using AutoPartsPOS.Application.Analytics.Dtos;
using AutoPartsPOS.Application.Common.Interfaces;
using AutoPartsPOS.Application.Inventory.Dtos;
using AutoPartsPOS.Application.Inventory.Interfaces;
using AutoPartsPOS.Application.Insights.Services;
using AutoPartsPOS.Application.Purchases.Dtos;
using AutoPartsPOS.Application.Purchases.Interfaces;
using AutoPartsPOS.Application.Purchases.Services;
using AutoPartsPOS.Application.Sales.Dtos;
using AutoPartsPOS.Application.Sales.Interfaces;
using AutoPartsPOS.Application.Sales.Services;
using AutoPartsPOS.Application.Settings.Dtos;
using AutoPartsPOS.Application.Settings.Interfaces;
using AutoPartsPOS.Application.Reports.Dtos;
using AutoPartsPOS.Application.Reports.Services;
using AutoPartsPOS.Application.Suppliers.Dtos;
using AutoPartsPOS.Application.Suppliers.Interfaces;
using AutoPartsPOS.Domain.Catalog;
using AutoPartsPOS.Domain.Common;
using AutoPartsPOS.Domain.Inventory;
using AutoPartsPOS.Domain.Purchases;
using AutoPartsPOS.Domain.Sales;
using AutoPartsPOS.Domain.Suppliers;
using AutoPartsPOS.WPF.Sales.Services;
using AutoPartsPOS.WPF.Reports.Services;
using System.IO;

namespace AutoPartsPOS.RuntimeValidation;

internal static class Program
{
    [STAThread]
    private static async Task Main(string[] args)
    {
        if (args.Contains("--sqlite-integration", StringComparer.OrdinalIgnoreCase))
        {
            await SqliteIntegrationValidation.RunAsync();
            return;
        }

        var product = new Product
        {
            ProductCode = "TEST-001",
            NameAr = "منتج اختبار",
            SellingPrice = 20,
            PurchasePrice = 10,
            CurrentAverageCost = 10,
            CurrentStock = 0,
            MinimumStock = 2,
            IsActive = true
        };
        SetId(product, 1);

        var supplier = new Supplier { NameAr = "مورد اختبار", IsActive = true };
        SetId(supplier, 1);

        var inventoryRepository = new FakeInventoryRepository(product);
        var purchaseRepository = new FakePurchaseInvoiceRepository();
        var salesRepository = new FakeSalesInvoiceRepository();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeDateTimeProvider();

        var purchaseService = new PurchaseInvoiceService(
            purchaseRepository,
            inventoryRepository,
            new FakeSupplierRepository(supplier),
            clock,
            unitOfWork);

        var purchaseResult = await purchaseService.CreateAsync(new CreatePurchaseInvoiceDto
        {
            InvoiceNumber = "PUR-TEST-001",
            SupplierId = supplier.Id,
            InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
            Items = [new CreatePurchaseInvoiceItemDto { ProductId = product.Id, Quantity = 10, UnitPrice = 10 }]
        });
        Assert(purchaseResult.Succeeded, "Purchase invoice creation failed.");
        Assert(product.CurrentStock == 10, "Purchase did not increase stock.");
        Assert(product.CurrentAverageCost == 10, "Purchase did not set weighted average cost.");
        Assert(inventoryRepository.Transactions.Any(item => item.TransactionType == InventoryTransactionType.Purchase && item.Quantity == 10), "Purchase ledger entry missing.");

        var salesService = new SalesInvoiceService(salesRepository, inventoryRepository, clock, unitOfWork);
        var salesResult = await salesService.CreateAsync(new CreateSalesInvoiceDto
        {
            InvoiceNumber = "SAL-TEST-001",
            InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
            DiscountAmount = 5,
            PaymentStatus = InvoicePaymentStatus.PartiallyPaid,
            PaidAmount = 30,
            Items = [new CreateSalesInvoiceItemDto { ProductId = product.Id, Quantity = 4, UnitPrice = 20 }]
        });
        Assert(salesResult.Succeeded, "Sales invoice creation failed.");
        Assert(product.CurrentStock == 6, "Sale did not decrease stock.");
        Assert(salesRepository.Invoices.Single().Items.Single().UnitCost == 10, "Sale did not store WAC unit cost snapshot.");
        Assert(salesRepository.Invoices.Single().Items.Single().TotalCost == 40, "Sale did not store WAC total cost snapshot.");
        Assert(inventoryRepository.Transactions.Any(item => item.TransactionType == InventoryTransactionType.Sale && item.Quantity == -4), "Sale ledger entry missing.");
        Assert(salesRepository.Invoices.Single().PaidAmount == 30 && salesRepository.Invoices.Single().RemainingAmount == 45,
            "Partial payment calculation failed.");
        var movementCountBeforePaymentEdit = inventoryRepository.Transactions.Count;
        Assert((await salesService.UpdatePaymentAsync(salesRepository.Invoices.Single().Id, InvoicePaymentStatus.Paid, 0)).Succeeded,
            "Payment edit failed.");
        Assert(inventoryRepository.Transactions.Count == movementCountBeforePaymentEdit,
            "Payment edit created a stock movement.");

        var oversellResult = await salesService.CreateAsync(new CreateSalesInvoiceDto
        {
            InvoiceNumber = "SAL-TEST-OVERSELL",
            InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
            PaymentStatus = InvoicePaymentStatus.Unpaid,
            Items = [new CreateSalesInvoiceItemDto { ProductId = product.Id, Quantity = 7, UnitPrice = 20 }]
        });
        Assert(!oversellResult.Succeeded, "Oversell validation failed.");
        Assert(product.CurrentStock == 6, "Oversell attempt changed stock.");

        var savedSale = salesRepository.Invoices.Single();
        var paidAmountBeforeVoid = savedSale.PaidAmount;
        var remainingAmountBeforeVoid = savedSale.RemainingAmount;
        var voidResult = await salesService.VoidAsync(savedSale.Id, "اختبار الإلغاء");
        Assert(voidResult.Succeeded, "Sales invoice void failed.");
        Assert(savedSale.Status == SalesInvoiceStatus.Voided, "Sales invoice status was not changed to voided.");
        Assert(product.CurrentStock == 10, "Voiding sale did not restore stock.");
        Assert(inventoryRepository.Transactions.Count(item => item.TransactionType == InventoryTransactionType.VoidSale && item.Quantity == 4) == 1, "Expected exactly one sales void ledger entry.");
        Assert(savedSale.PaidAmount == paidAmountBeforeVoid && savedSale.RemainingAmount == remainingAmountBeforeVoid,
            "Voiding a sale changed its payment tracking values.");
        Assert(unitOfWork.TransactionCount == 3, "Expected purchase, sale, and void operations to be transactional.");

        var movementCountAfterVoid = inventoryRepository.Transactions.Count;
        var duplicateVoidResult = await salesService.VoidAsync(savedSale.Id, "اختبار إلغاء مكرر");
        Assert(!duplicateVoidResult.Succeeded, "A sales invoice was cancelled twice.");
        Assert(product.CurrentStock == 10, "Duplicate cancellation changed stock.");
        Assert(inventoryRepository.Transactions.Count == movementCountAfterVoid, "Duplicate cancellation created an inventory movement.");
        Assert(unitOfWork.TransactionCount == 3, "Duplicate cancellation opened a stock transaction.");

        var details = await salesRepository.GetDetailsAsync(savedSale.Id) ?? throw new InvalidOperationException("Saved sales invoice details missing.");
        var printService = new SalesInvoicePrintService(new FakeApplicationSettingsService());
        var document = await printService.CreatePreviewDocumentAsync(details);
        Assert(document.Blocks.Count > 0, "Arabic print document was not generated.");
        Assert(ReportCalculations.CalculateProfit(100, 65) == 35, "Profit calculation failed.");
        Assert(SmartInsightCalculations.CalculateExpectedMonthlySales(15, 30) == 15, "Expected monthly sales calculation failed.");
        Assert(SmartInsightCalculations.CalculateSuggestedQuantity(15, 6) == 9, "Reorder suggestion calculation failed.");

        var exportService = new ReportExportService();
        var exportDirectory = Path.Combine(Path.GetTempPath(), "AutoPartsPOS-Validation");
        Directory.CreateDirectory(exportDirectory);
        var pdfPath = Path.Combine(exportDirectory, "inventory-report.pdf");
        var excelPath = Path.Combine(exportDirectory, "inventory-report.xlsx");
        var inventoryReport = new InventoryReportDto(10, 100, 1,
        [
            new InventoryReportItemDto(product.Id, product.ProductCode, product.NameAr, product.CurrentStock, product.CurrentAverageCost, product.CurrentStock * product.CurrentAverageCost, product.MinimumStock, true)
        ]);
        await exportService.ExportInventoryReportToPdfAsync(inventoryReport, pdfPath);
        await exportService.ExportInventoryReportToExcelAsync(inventoryReport, excelPath);
        Assert(new FileInfo(pdfPath).Length > 0, "PDF export file was not generated.");
        Assert(new FileInfo(excelPath).Length > 0, "Excel export file was not generated.");
        File.Delete(pdfPath);
        File.Delete(excelPath);

        if (args.Contains("--preview", StringComparer.OrdinalIgnoreCase))
        {
            Exception? previewException = null;
            var previewThread = new Thread(() =>
            {
                try
                {
                    SynchronizationContext.SetSynchronizationContext(
                        new System.Windows.Threading.DispatcherSynchronizationContext(
                            System.Windows.Threading.Dispatcher.CurrentDispatcher));
                    _ = new System.Windows.Application();
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(2)
                    };
                    timer.Tick += (_, _) =>
                    {
                        timer.Stop();

                        foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
                        {
                            window.Close();
                        }
                    };
                    timer.Start();
                    printService.PreviewAsync(details).GetAwaiter().GetResult();
                }
                catch (Exception exception)
                {
                    previewException = exception;
                }
            });
            previewThread.SetApartmentState(ApartmentState.STA);
            previewThread.Start();
            previewThread.Join(TimeSpan.FromSeconds(15));
            Assert(!previewThread.IsAlive, "Print preview window did not close cleanly.");
            Assert(previewException is null, $"Print preview failed: {previewException}");
            Console.WriteLine("PRINT_PREVIEW_WINDOW:PASS");
        }

        Console.WriteLine("PURCHASE_STOCK_INCREASE:PASS");
        Console.WriteLine("SALE_STOCK_DECREASE:PASS");
        Console.WriteLine("OVERSELL_REJECTED:PASS");
        Console.WriteLine("VOID_SALE_STOCK_RESTORE:PASS");
        Console.WriteLine("VOID_SALE_DUPLICATE_PROTECTION:PASS");
        Console.WriteLine("VOID_SALE_PAYMENT_VALUES_PRESERVED:PASS");
        Console.WriteLine("LEDGER_PURCHASE_SALE_VOIDSALE:PASS");
        Console.WriteLine("TRANSACTION_BOUNDARIES:PASS");
        Console.WriteLine("PRINT_DOCUMENT_GENERATION:PASS");
        Console.WriteLine("REPORT_CALCULATIONS:PASS");
        Console.WriteLine("SMART_INSIGHT_CALCULATIONS:PASS");
        Console.WriteLine("PDF_EXCEL_EXPORTS:PASS");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void SetId(object entity, long id)
    {
        entity.GetType().BaseType!.GetProperty("Id")!.SetValue(entity, id);
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset Now => DateTimeOffset.UtcNow;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int TransactionCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

        public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
        {
            TransactionCount++;
            await operation(cancellationToken);
        }
    }

    private sealed class FakeSupplierRepository(Supplier supplier) : ISupplierRepository
    {
        public Task<IReadOnlyList<SupplierDto>> SearchAsync(string? searchText, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SupplierDto>>([]);

        public Task<Supplier?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(id == supplier.Id ? supplier : null);

        public Task<bool> NameExistsAsync(string nameAr, long? excludedId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task AddAsync(Supplier entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeInventoryRepository(Product product) : IInventoryRepository
    {
        public List<InventoryTransaction> Transactions { get; } = [];

        public Task<IReadOnlyList<InventoryTransactionDto>> SearchAsync(string? searchText, long? productId, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryTransactionDto>>([]);

        public Task<Product?> GetProductForUpdateAsync(long productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Product?>(productId == product.Id ? product : null);

        public Task AddTransactionAsync(InventoryTransaction transaction, CancellationToken cancellationToken = default)
        {
            SetId(transaction, Transactions.Count + 1);
            Transactions.Add(transaction);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePurchaseInvoiceRepository : IPurchaseInvoiceRepository
    {
        private readonly List<PurchaseInvoice> _invoices = [];

        public Task<IReadOnlyList<PurchaseInvoiceListDto>> SearchAsync(string? searchText, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PurchaseInvoiceListDto>>([]);

        public Task<PurchaseInvoiceDetailsDto?> GetDetailsAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult<PurchaseInvoiceDetailsDto?>(null);

        public Task<PurchaseInvoice?> GetByIdWithItemsAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_invoices.SingleOrDefault(item => item.Id == id));

        public Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(_invoices.Any(item => item.InvoiceNumber == invoiceNumber));

        public Task AddAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default)
        {
            SetId(invoice, _invoices.Count + 1);
            _invoices.Add(invoice);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSalesInvoiceRepository : ISalesInvoiceRepository
    {
        public List<SalesInvoice> Invoices { get; } = [];

        public Task<IReadOnlyList<SalesInvoiceListDto>> SearchAsync(string? searchText, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SalesInvoiceListDto>>(Invoices.Select(ToListDto).ToList());

        public Task<SalesInvoiceDetailsDto?> GetDetailsAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Invoices.Where(item => item.Id == id).Select(ToDetailsDto).SingleOrDefault());

        public Task<SalesInvoice?> GetByIdWithItemsAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Invoices.SingleOrDefault(item => item.Id == id));

        public Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(Invoices.Any(item => item.InvoiceNumber == invoiceNumber));

        public Task AddAsync(SalesInvoice invoice, CancellationToken cancellationToken = default)
        {
            SetId(invoice, Invoices.Count + 1);
            foreach (var item in invoice.Items)
            {
                item.Product = null;
                SetId(item, item.ProductId);
            }
            Invoices.Add(invoice);
            return Task.CompletedTask;
        }

        private static SalesInvoiceListDto ToListDto(SalesInvoice invoice) =>
            new(invoice.Id, invoice.InvoiceNumber, invoice.InvoiceDate, invoice.Status.ToString(), invoice.SubtotalAmount, invoice.DiscountAmount, invoice.NetTotalAmount, invoice.PaidAmount, invoice.RemainingAmount, invoice.Notes);

        private static SalesInvoiceDetailsDto ToDetailsDto(SalesInvoice invoice) =>
            new(invoice.Id, invoice.InvoiceNumber, invoice.InvoiceDate, invoice.Status.ToString(), invoice.PaymentStatus, invoice.Status == SalesInvoiceStatus.Voided, invoice.SubtotalAmount, invoice.DiscountAmount, invoice.NetTotalAmount, invoice.PaidAmount, invoice.RemainingAmount, invoice.Notes,
                invoice.Items.Select(item => new SalesInvoiceItemDto(item.Id, item.ProductId, "منتج اختبار", item.Quantity, item.UnitPrice, item.TotalPrice, item.UnitCost, item.TotalCost)).ToList());
    }

    private sealed class FakeApplicationSettingsService : IApplicationSettingsService
    {
        public Task<ApplicationSettingsDto> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new ApplicationSettingsDto { StoreName = "متجر الاختبار", CurrencySymbol = "ج.م" });

        public Task<AutoPartsPOS.Application.Common.Models.OperationResult> SaveAsync(ApplicationSettingsDto settings, CancellationToken cancellationToken = default) =>
            Task.FromResult(AutoPartsPOS.Application.Common.Models.OperationResult.Success());

        public Task<ApplicationSettingsDto> ResetToDefaultsAsync(CancellationToken cancellationToken = default) => LoadAsync(cancellationToken);
    }
}
