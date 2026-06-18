using AutoPartsPOS.Application.Inventory.Dtos;
using AutoPartsPOS.Application.Inventory.Interfaces;
using AutoPartsPOS.Domain.Catalog;
using AutoPartsPOS.Domain.Inventory;
using AutoPartsPOS.Domain.Purchases;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsPOS.Persistence.Inventory;

public sealed class InventoryRepository(AppDbContext dbContext) : IInventoryRepository
{
    public async Task<IReadOnlyList<InventoryTransactionDto>> SearchAsync(string? searchText, long? productId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.InventoryTransactions
            .AsNoTracking()
            .Include(transaction => transaction.Product)
            .AsQueryable();

        if (productId is > 0)
        {
            query = query.Where(transaction => transaction.ProductId == productId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var term = searchText.Trim();
            query = query.Where(transaction =>
                transaction.Product != null && EF.Functions.Like(transaction.Product.NameAr, $"%{term}%"));
        }

        var transactions = (await query.ToListAsync(cancellationToken))
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.Id)
            .ToList();

        var purchaseReferenceIds = transactions
            .Where(transaction => transaction.ReferenceType == InventoryReferenceType.PurchaseInvoice)
            .Select(transaction => transaction.ReferenceId)
            .Distinct()
            .ToArray();
        var salesReferenceIds = transactions
            .Where(transaction => transaction.ReferenceType == InventoryReferenceType.SalesInvoice)
            .Select(transaction => transaction.ReferenceId)
            .Distinct()
            .ToArray();

        var purchaseReferences = await dbContext.PurchaseInvoices
            .AsNoTracking()
            .Where(invoice => purchaseReferenceIds.Contains(invoice.Id))
            .ToDictionaryAsync(invoice => invoice.Id, invoice => invoice.InvoiceNumber, cancellationToken);
        var salesReferences = await dbContext.SalesInvoices
            .AsNoTracking()
            .Where(invoice => salesReferenceIds.Contains(invoice.Id))
            .ToDictionaryAsync(invoice => invoice.Id, invoice => invoice.InvoiceNumber, cancellationToken);

        return transactions
            .Select(transaction => new InventoryTransactionDto(
                transaction.Id,
                transaction.ProductId,
                transaction.Product?.NameAr ?? string.Empty,
                GetTransactionTypeText(transaction.TransactionType),
                transaction.Quantity,
                transaction.BalanceAfter,
                GetReferenceTypeText(transaction.ReferenceType),
                transaction.ReferenceId,
                GetReferenceNumber(transaction, purchaseReferences, salesReferences),
                transaction.TransactionDate,
                transaction.Notes))
            .ToList();
    }

    public Task<Product?> GetProductForUpdateAsync(long productId, CancellationToken cancellationToken = default)
    {
        return dbContext.Products.SingleOrDefaultAsync(product => product.Id == productId, cancellationToken);
    }

    public Task AddTransactionAsync(InventoryTransaction transaction, CancellationToken cancellationToken = default)
    {
        return dbContext.InventoryTransactions.AddAsync(transaction, cancellationToken).AsTask();
    }

    private static string GetTransactionTypeText(InventoryTransactionType transactionType)
    {
        return transactionType switch
        {
            InventoryTransactionType.Purchase => "شراء",
            InventoryTransactionType.Sale => "بيع",
            InventoryTransactionType.Adjustment => "تسوية",
            InventoryTransactionType.VoidPurchase => "إلغاء شراء",
            InventoryTransactionType.VoidSale => "إلغاء بيع",
            _ => transactionType.ToString()
        };
    }

    private static string GetReferenceTypeText(InventoryReferenceType referenceType)
    {
        return referenceType switch
        {
            InventoryReferenceType.PurchaseInvoice => "فاتورة شراء",
            InventoryReferenceType.SalesInvoice => "فاتورة بيع",
            InventoryReferenceType.ManualAdjustment => "تسوية يدوية",
            _ => referenceType.ToString()
        };
    }

    private static string GetReferenceNumber(
        InventoryTransaction transaction,
        IReadOnlyDictionary<long, string> purchaseReferences,
        IReadOnlyDictionary<long, string> salesReferences)
    {
        if (transaction.ReferenceType == InventoryReferenceType.PurchaseInvoice &&
            purchaseReferences.TryGetValue(transaction.ReferenceId, out var invoiceNumber))
        {
            return invoiceNumber;
        }

        if (transaction.ReferenceType == InventoryReferenceType.SalesInvoice &&
            salesReferences.TryGetValue(transaction.ReferenceId, out var salesInvoiceNumber))
        {
            return salesInvoiceNumber;
        }

        return transaction.ReferenceId.ToString();
    }
}
