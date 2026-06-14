using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.Domain.Catalog;

namespace AutoPartsPOS.Application.Catalog.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyList<ProductDto>> SearchAsync(string? searchText, long? categoryId, CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<bool> ProductCodeExistsAsync(string productCode, long? excludedId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Product product, CancellationToken cancellationToken = default);
}
