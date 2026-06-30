using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.Application.Catalog.Interfaces;
using AutoPartsPOS.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsPOS.Persistence.Catalog;

public sealed class CategoryRepository(AppDbContext dbContext) : ICategoryRepository
{
    public async Task<IReadOnlyList<CategoryDto>> SearchAsync(string? searchText, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<ProductCategory>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var term = searchText.Trim();
            query = query.Where(category =>
                EF.Functions.Like(category.NameAr, $"%{term}%") ||
                (category.Description != null && EF.Functions.Like(category.Description, $"%{term}%")));
        }

        return await query
            .OrderByDescending(category => category.IsActive)
            .ThenBy(category => category.NameAr)
            .Select(category => new CategoryDto(
                category.Id,
                category.NameAr,
                category.Description,
                category.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryLookupDto>> GetActiveLookupAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<ProductCategory>()
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.NameAr)
            .Select(category => new CategoryLookupDto(category.Id, category.NameAr))
            .ToListAsync(cancellationToken);
    }

    public Task<ProductCategory?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<ProductCategory>()
            .SingleOrDefaultAsync(category => category.Id == id, cancellationToken);
    }

    public Task<bool> NameExistsAsync(string nameAr, long? excludedId = null, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<ProductCategory>()
            .AnyAsync(category =>
                category.NameAr == nameAr &&
                (excludedId == null || category.Id != excludedId.Value),
                cancellationToken);
    }

    public Task AddAsync(ProductCategory category, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<ProductCategory>().AddAsync(category, cancellationToken).AsTask();
    }

    public Task<bool> HasProductsAsync(long id, CancellationToken cancellationToken = default) =>
        dbContext.Products.AnyAsync(product => product.CategoryId == id, cancellationToken);

    public void Delete(ProductCategory category) =>
        dbContext.ProductCategories.Remove(category);
}

