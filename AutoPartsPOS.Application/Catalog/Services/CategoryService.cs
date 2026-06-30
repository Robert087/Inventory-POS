using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.Application.Catalog.Interfaces;
using AutoPartsPOS.Application.Common.Interfaces;
using AutoPartsPOS.Application.Common.Models;
using AutoPartsPOS.Domain.Catalog;
using System.Text;

namespace AutoPartsPOS.Application.Catalog.Services;

public sealed class CategoryService(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork) : ICategoryService
{
    public Task<IReadOnlyList<CategoryDto>> SearchAsync(string? searchText, CancellationToken cancellationToken = default)
    {
        return categoryRepository.SearchAsync(searchText, cancellationToken);
    }

    public Task<IReadOnlyList<CategoryLookupDto>> GetActiveLookupAsync(CancellationToken cancellationToken = default)
    {
        return categoryRepository.GetActiveLookupAsync(cancellationToken);
    }

    public async Task<CategoryDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var category = await categoryRepository.GetByIdAsync(id, cancellationToken);
        return category is null
            ? null
            : new CategoryDto(category.Id, category.NameAr, category.Description, category.IsActive);
    }

    public async Task<OperationResult> SaveAsync(SaveCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var errors = await ValidateAsync(dto, cancellationToken);

        if (errors.Count > 0)
        {
            return OperationResult.Failure(errors);
        }

        if (dto.Id is null)
        {
            var category = new ProductCategory
            {
                NameAr = Normalize(dto.NameAr),
                Description = NormalizeNullable(dto.Description),
                IsActive = dto.IsActive
            };

            await categoryRepository.AddAsync(category, cancellationToken);
        }
        else
        {
            var category = await categoryRepository.GetByIdAsync(dto.Id.Value, cancellationToken);

            if (category is null)
            {
                AddError(errors, string.Empty, "التصنيف غير موجود.");
                return OperationResult.Failure(errors);
            }

            category.NameAr = Normalize(dto.NameAr);
            category.Description = NormalizeNullable(dto.Description);
            category.IsActive = dto.IsActive;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeactivateAsync(long id, CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, List<string>>();
        var category = await categoryRepository.GetByIdAsync(id, cancellationToken);

        if (category is null)
        {
            AddError(errors, string.Empty, "التصنيف غير موجود.");
            return OperationResult.Failure(errors);
        }

        category.IsActive = false;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, List<string>>();
        var category = await categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            AddError(errors, string.Empty, "التصنيف غير موجود.");
            return OperationResult.Failure(errors);
        }

        if (await categoryRepository.HasProductsAsync(id, cancellationToken))
        {
            AddError(errors, string.Empty, "لا يمكن حذف التصنيف لأنه مرتبط بصنف واحد أو أكثر. يمكنك تعطيله من شاشة التعديل بدلًا من حذفه.");
            return OperationResult.Failure(errors);
        }

        categoryRepository.Delete(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    private async Task<Dictionary<string, List<string>>> ValidateAsync(SaveCategoryDto dto, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, List<string>>();
        var nameAr = Normalize(dto.NameAr);

        if (string.IsNullOrWhiteSpace(nameAr))
        {
            AddError(errors, nameof(SaveCategoryDto.NameAr), "اسم التصنيف مطلوب.");
        }
        else if (await categoryRepository.NameExistsAsync(nameAr, dto.Id, cancellationToken))
        {
            AddError(errors, nameof(SaveCategoryDto.NameAr), "اسم التصنيف مستخدم من قبل.");
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
