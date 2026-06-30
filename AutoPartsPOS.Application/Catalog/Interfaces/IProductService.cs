using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.Application.Common.Models;

namespace AutoPartsPOS.Application.Catalog.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> SearchAsync(string? searchText, long? categoryId, CancellationToken cancellationToken = default);

    Task<ProductDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<ProductDto?> GetByProductCodeAsync(string productCode, CancellationToken cancellationToken = default);

    Task<OperationResult> SaveAsync(SaveProductDto dto, CancellationToken cancellationToken = default);

    Task<OperationResult> ReplenishStockAsync(ReplenishProductStockDto dto, CancellationToken cancellationToken = default);

    Task<OperationResult> DeactivateAsync(long id, CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteAsync(long id, CancellationToken cancellationToken = default);
}
