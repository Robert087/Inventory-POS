using AutoPartsPOS.Application.Suppliers.Dtos;
using AutoPartsPOS.Application.Suppliers.Interfaces;
using AutoPartsPOS.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsPOS.Persistence.Suppliers;

public sealed class SupplierRepository(AppDbContext dbContext) : ISupplierRepository
{
    public async Task<IReadOnlyList<SupplierDto>> SearchAsync(string? searchText, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Supplier>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var term = searchText.Trim();
            query = query.Where(supplier =>
                EF.Functions.Like(supplier.NameAr, $"%{term}%") ||
                (supplier.Phone != null && EF.Functions.Like(supplier.Phone, $"%{term}%")) ||
                (supplier.Address != null && EF.Functions.Like(supplier.Address, $"%{term}%")));
        }

        return await query
            .OrderByDescending(supplier => supplier.IsActive)
            .ThenBy(supplier => supplier.NameAr)
            .Select(supplier => new SupplierDto(
                supplier.Id,
                supplier.NameAr,
                supplier.Phone,
                supplier.Address,
                supplier.Notes,
                supplier.IsActive))
            .ToListAsync(cancellationToken);
    }

    public Task<Supplier?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<Supplier>()
            .SingleOrDefaultAsync(supplier => supplier.Id == id, cancellationToken);
    }

    public Task<bool> NameExistsAsync(string nameAr, long? excludedId = null, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<Supplier>()
            .AnyAsync(supplier =>
                supplier.NameAr == nameAr &&
                (excludedId == null || supplier.Id != excludedId.Value),
                cancellationToken);
    }

    public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<Supplier>().AddAsync(supplier, cancellationToken).AsTask();
    }

    public Task<bool> HasPurchaseInvoicesAsync(long id, CancellationToken cancellationToken = default) =>
        dbContext.PurchaseInvoices.AnyAsync(invoice => invoice.SupplierId == id, cancellationToken);

    public void Delete(Supplier supplier) =>
        dbContext.Suppliers.Remove(supplier);
}

