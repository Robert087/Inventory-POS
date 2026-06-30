using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.Domain.Catalog;

namespace AutoPartsPOS.Application.Catalog.Interfaces;

public interface ICategoryRepository
{
    Task<IReadOnlyList<CategoryDto>> SearchAsync(string? searchText, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryLookupDto>> GetActiveLookupAsync(CancellationToken cancellationToken = default);

    Task<ProductCategory?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string nameAr, long? excludedId = null, CancellationToken cancellationToken = default);

    Task AddAsync(ProductCategory category, CancellationToken cancellationToken = default);

    Task<bool> HasProductsAsync(long id, CancellationToken cancellationToken = default);

    void Delete(ProductCategory category);
}
