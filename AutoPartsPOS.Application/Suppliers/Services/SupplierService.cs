using AutoPartsPOS.Application.Common.Interfaces;
using AutoPartsPOS.Application.Common.Models;
using AutoPartsPOS.Application.Suppliers.Dtos;
using AutoPartsPOS.Application.Suppliers.Interfaces;
using AutoPartsPOS.Domain.Suppliers;
using System.Text;

namespace AutoPartsPOS.Application.Suppliers.Services;

public sealed class SupplierService(
    ISupplierRepository supplierRepository,
    IUnitOfWork unitOfWork) : ISupplierService
{
    public Task<IReadOnlyList<SupplierDto>> SearchAsync(string? searchText, CancellationToken cancellationToken = default)
    {
        return supplierRepository.SearchAsync(searchText, cancellationToken);
    }

    public async Task<SupplierDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var supplier = await supplierRepository.GetByIdAsync(id, cancellationToken);

        return supplier is null
            ? null
            : new SupplierDto(supplier.Id, supplier.NameAr, supplier.Phone, supplier.Address, supplier.Notes, supplier.IsActive);
    }

    public async Task<OperationResult> SaveAsync(SaveSupplierDto dto, CancellationToken cancellationToken = default)
    {
        var errors = await ValidateAsync(dto, cancellationToken);

        if (errors.Count > 0)
        {
            return OperationResult.Failure(errors);
        }

        if (dto.Id is null)
        {
            var supplier = new Supplier
            {
                NameAr = Normalize(dto.NameAr),
                Phone = NormalizeNullable(dto.Phone),
                Address = NormalizeNullable(dto.Address),
                Notes = NormalizeNullable(dto.Notes),
                IsActive = dto.IsActive
            };

            await supplierRepository.AddAsync(supplier, cancellationToken);
        }
        else
        {
            var supplier = await supplierRepository.GetByIdAsync(dto.Id.Value, cancellationToken);

            if (supplier is null)
            {
                AddError(errors, string.Empty, "المورد غير موجود.");
                return OperationResult.Failure(errors);
            }

            supplier.NameAr = Normalize(dto.NameAr);
            supplier.Phone = NormalizeNullable(dto.Phone);
            supplier.Address = NormalizeNullable(dto.Address);
            supplier.Notes = NormalizeNullable(dto.Notes);
            supplier.IsActive = dto.IsActive;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeactivateAsync(long id, CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, List<string>>();
        var supplier = await supplierRepository.GetByIdAsync(id, cancellationToken);

        if (supplier is null)
        {
            AddError(errors, string.Empty, "المورد غير موجود.");
            return OperationResult.Failure(errors);
        }

        supplier.IsActive = false;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    private async Task<Dictionary<string, List<string>>> ValidateAsync(SaveSupplierDto dto, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, List<string>>();
        var nameAr = Normalize(dto.NameAr);

        if (string.IsNullOrWhiteSpace(nameAr))
        {
            AddError(errors, nameof(SaveSupplierDto.NameAr), "اسم المورد مطلوب.");
        }
        else if (await supplierRepository.NameExistsAsync(nameAr, dto.Id, cancellationToken))
        {
            AddError(errors, nameof(SaveSupplierDto.NameAr), "اسم المورد مستخدم من قبل.");
        }

        return errors;
    }

    private static string Normalize(string value)
    {
        return value.Trim().Normalize(NormalizationForm.FormC);
    }

    private static string? NormalizeNullable(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized.Normalize(NormalizationForm.FormC);
    }

    private static void AddError(Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var messages))
        {
            messages = [];
            errors[key] = messages;
        }

        messages.Add(message);
    }
}
