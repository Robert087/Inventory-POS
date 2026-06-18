using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.Application.Catalog.Interfaces;
using AutoPartsPOS.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsPOS.Persistence.Catalog;

public sealed class ProductRepository(AppDbContext dbContext) : IProductRepository
{
    public async Task<IReadOnlyList<ProductDto>> SearchAsync(string? searchText, long? categoryId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Product>()
            .AsNoTracking()
            .Include(product => product.Category)
            .AsQueryable();

        if (categoryId is > 0)
        {
            query = query.Where(product => product.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var term = searchText.Trim();
            query = query.Where(product =>
                EF.Functions.Like(product.ProductCode, $"%{term}%") ||
                EF.Functions.Like(product.NameAr, $"%{term}%") ||
                (product.Barcode != null && EF.Functions.Like(product.Barcode, $"%{term}%")));
        }

        return await query
            .OrderByDescending(product => product.IsActive)
            .ThenBy(product => product.NameAr)
            .Select(product => new ProductDto(
                product.Id,
                product.ProductCode,
                product.Barcode,
                product.NameAr,
                product.CategoryId,
                product.Category == null ? string.Empty : product.Category.NameAr,
                product.PurchasePrice,
                product.CurrentAverageCost,
                product.SellingPrice,
                product.CurrentStock,
                product.MinimumStock,
                product.IsActive))
            .ToListAsync(cancellationToken);
    }

    public Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<Product>()
            .Include(product => product.Category)
            .SingleOrDefaultAsync(product => product.Id == id, cancellationToken);
    }

    public Task<bool> ProductCodeExistsAsync(string productCode, long? excludedId = null, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<Product>()
            .AnyAsync(product =>
                product.ProductCode == productCode &&
                (excludedId == null || product.Id != excludedId.Value),
                cancellationToken);
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<Product>().AddAsync(product, cancellationToken).AsTask();
    }
}

