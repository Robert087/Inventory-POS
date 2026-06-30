using AutoPartsPOS.Application.Suppliers.Dtos;
using AutoPartsPOS.Domain.Suppliers;

namespace AutoPartsPOS.Application.Suppliers.Interfaces;

public interface ISupplierRepository
{
    Task<IReadOnlyList<SupplierDto>> SearchAsync(string? searchText, CancellationToken cancellationToken = default);

    Task<Supplier?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string nameAr, long? excludedId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default);

    Task<bool> HasPurchaseInvoicesAsync(long id, CancellationToken cancellationToken = default);

    void Delete(Supplier supplier);
}
