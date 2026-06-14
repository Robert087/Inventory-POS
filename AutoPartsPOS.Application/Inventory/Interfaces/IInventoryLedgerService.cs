using AutoPartsPOS.Application.Inventory.Dtos;

namespace AutoPartsPOS.Application.Inventory.Interfaces;

public interface IInventoryLedgerService
{
    Task<IReadOnlyList<InventoryTransactionDto>> SearchAsync(string? searchText, long? productId, CancellationToken cancellationToken = default);
}
