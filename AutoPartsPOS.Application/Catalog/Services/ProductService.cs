using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.Application.Catalog.Interfaces;
using AutoPartsPOS.Application.Common.Interfaces;
using AutoPartsPOS.Application.Common.Models;
using AutoPartsPOS.Domain.Catalog;
using System.Text;

namespace AutoPartsPOS.Application.Catalog.Services;

public sealed class ProductService(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork) : IProductService
{
    public Task<IReadOnlyList<ProductDto>> SearchAsync(string? searchText, long? categoryId, CancellationToken cancellationToken = default)
    {
        return productRepository.SearchAsync(searchText, categoryId, cancellationToken);
    }

    public async Task<ProductDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken);

        if (product is null)
        {
            return null;
        }

        return new ProductDto(
            product.Id,
            product.ProductCode,
            product.Barcode,
            product.NameAr,
            product.CategoryId,
            product.Category?.NameAr ?? string.Empty,
            product.PurchasePrice,
            product.CurrentAverageCost,
            product.SellingPrice,
            product.CurrentStock,
            product.MinimumStock,
            product.IsActive);
    }

    public async Task<OperationResult> SaveAsync(SaveProductDto dto, CancellationToken cancellationToken = default)
    {
        var errors = await ValidateAsync(dto, cancellationToken);

        if (errors.Count > 0)
        {
            return OperationResult.Failure(errors);
        }

        if (dto.Id is null)
        {
            var product = new Product
            {
                ProductCode = Normalize(dto.ProductCode),
                Barcode = NormalizeNullable(dto.Barcode),
                NameAr = Normalize(dto.NameAr),
                CategoryId = dto.CategoryId,
                PurchasePrice = dto.PurchasePrice,
                CurrentAverageCost = dto.PurchasePrice,
                SellingPrice = dto.SellingPrice,
                CurrentStock = dto.CurrentStock,
                MinimumStock = dto.MinimumStock,
                IsActive = dto.IsActive
            };

            await productRepository.AddAsync(product, cancellationToken);
        }
        else
        {
            var product = await productRepository.GetByIdAsync(dto.Id.Value, cancellationToken);

            if (product is null)
            {
                AddError(errors, string.Empty, "المنتج غير موجود.");
                return OperationResult.Failure(errors);
            }

            product.ProductCode = Normalize(dto.ProductCode);
            product.Barcode = NormalizeNullable(dto.Barcode);
            product.NameAr = Normalize(dto.NameAr);
            product.CategoryId = dto.CategoryId;
            product.PurchasePrice = dto.PurchasePrice;
            product.SellingPrice = dto.SellingPrice;
            product.CurrentStock = dto.CurrentStock;
            product.MinimumStock = dto.MinimumStock;
            product.IsActive = dto.IsActive;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeactivateAsync(long id, CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, List<string>>();
        var product = await productRepository.GetByIdAsync(id, cancellationToken);

        if (product is null)
        {
            AddError(errors, string.Empty, "المنتج غير موجود.");
            return OperationResult.Failure(errors);
        }

        product.IsActive = false;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    private async Task<Dictionary<string, List<string>>> ValidateAsync(SaveProductDto dto, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, List<string>>();
        var productCode = Normalize(dto.ProductCode);
        var nameAr = Normalize(dto.NameAr);

        if (string.IsNullOrWhiteSpace(productCode))
        {
            AddError(errors, nameof(SaveProductDto.ProductCode), "كود المنتج مطلوب.");
        }
        else if (await productRepository.ProductCodeExistsAsync(productCode, dto.Id, cancellationToken))
        {
            AddError(errors, nameof(SaveProductDto.ProductCode), "كود المنتج مستخدم من قبل.");
        }

        if (string.IsNullOrWhiteSpace(nameAr))
        {
            AddError(errors, nameof(SaveProductDto.NameAr), "اسم المنتج مطلوب.");
        }

        if (dto.CategoryId <= 0 || await categoryRepository.GetByIdAsync(dto.CategoryId, cancellationToken) is null)
        {
            AddError(errors, nameof(SaveProductDto.CategoryId), "يجب اختيار تصنيف صحيح.");
        }

        if (dto.PurchasePrice < 0)
        {
            AddError(errors, nameof(SaveProductDto.PurchasePrice), "سعر الشراء لا يمكن أن يكون أقل من صفر.");
        }

        if (dto.SellingPrice < 0)
        {
            AddError(errors, nameof(SaveProductDto.SellingPrice), "سعر البيع لا يمكن أن يكون أقل من صفر.");
        }

        if (dto.CurrentStock < 0)
        {
            AddError(errors, nameof(SaveProductDto.CurrentStock), "المخزون الحالي لا يمكن أن يكون أقل من صفر.");
        }

        if (dto.MinimumStock < 0)
        {
            AddError(errors, nameof(SaveProductDto.MinimumStock), "الحد الأدنى للمخزون لا يمكن أن يكون أقل من صفر.");
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
