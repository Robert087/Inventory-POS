using AutoPartsPOS.Application.Inventory.Dtos;
using AutoPartsPOS.Application.Inventory.Interfaces;

namespace AutoPartsPOS.Application.Inventory.Services;

public sealed class InventoryLedgerService(IInventoryRepository inventoryRepository) : IInventoryLedgerService
{
    public Task<IReadOnlyList<InventoryTransactionDto>> SearchAsync(
        string? searchText,
        long? productId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        return inventoryRepository.SearchAsync(searchText, productId, fromDate, toDate, cancellationToken);
    }
}
