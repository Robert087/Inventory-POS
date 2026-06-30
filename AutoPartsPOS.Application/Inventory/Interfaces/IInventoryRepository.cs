using AutoPartsPOS.Application.Inventory.Dtos;
using AutoPartsPOS.Domain.Catalog;
using AutoPartsPOS.Domain.Inventory;

namespace AutoPartsPOS.Application.Inventory.Interfaces;

public interface IInventoryRepository
{
    Task<IReadOnlyList<InventoryTransactionDto>> SearchAsync(
        string? searchText,
        long? productId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);

    Task<Product?> GetProductForUpdateAsync(long productId, CancellationToken cancellationToken = default);

    Task AddTransactionAsync(InventoryTransaction transaction, CancellationToken cancellationToken = default);
}
