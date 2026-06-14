using AutoPartsPOS.Application.Common.Models;
using AutoPartsPOS.Application.Suppliers.Dtos;

namespace AutoPartsPOS.Application.Suppliers.Interfaces;

public interface ISupplierService
{
    Task<IReadOnlyList<SupplierDto>> SearchAsync(string? searchText, CancellationToken cancellationToken = default);

    Task<SupplierDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<OperationResult> SaveAsync(SaveSupplierDto dto, CancellationToken cancellationToken = default);

    Task<OperationResult> DeactivateAsync(long id, CancellationToken cancellationToken = default);
}
