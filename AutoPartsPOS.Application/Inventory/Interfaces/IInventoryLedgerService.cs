using AutoPartsPOS.Application.Inventory.Dtos;

namespace AutoPartsPOS.Application.Inventory.Interfaces;

public interface IInventoryLedgerService
{
    Task<IReadOnlyList<InventoryTransactionDto>> SearchAsync(
        string? searchText,
        long? productId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);
}
