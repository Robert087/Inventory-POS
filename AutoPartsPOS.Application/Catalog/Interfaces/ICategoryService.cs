using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.Application.Common.Models;

namespace AutoPartsPOS.Application.Catalog.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> SearchAsync(string? searchText, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryLookupDto>> GetActiveLookupAsync(CancellationToken cancellationToken = default);

    Task<CategoryDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<OperationResult> SaveAsync(SaveCategoryDto dto, CancellationToken cancellationToken = default);

    Task<OperationResult> DeactivateAsync(long id, CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteAsync(long id, CancellationToken cancellationToken = default);
}
