using AutoPartsPOS.Application.Catalog.Interfaces;
using AutoPartsPOS.Application.Common.Interfaces;
using AutoPartsPOS.Application.Common.Models;
using AutoPartsPOS.Application.LatestPrices.Dtos;
using AutoPartsPOS.Application.LatestPrices.Interfaces;
using AutoPartsPOS.Domain.Catalog;
using System.Text;

namespace AutoPartsPOS.Application.LatestPrices.Services;

public sealed class LatestPriceService(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork) : ILatestPriceService
{
    public Task<IReadOnlyList<LatestPriceDto>> SearchAsync(string? searchText, CancellationToken cancellationToken = default)
    {
        return productRepository.SearchLatestPricesAsync(searchText, cancellationToken);
    }

    public async Task<OperationResult> SaveAsync(SaveLatestPriceDto dto, CancellationToken cancellationToken = default)
    {
        var errors = await ValidateAsync(dto, cancellationToken);

        if (errors.Count > 0)
        {
            return OperationResult.Failure(errors);
        }

        var productCode = Normalize(dto.ProductCode);
        var nameAr = Normalize(dto.NameAr);
        Product? product = null;

        if (dto.Id is > 0)
        {
            product = await productRepository.GetByIdAsync(dto.Id.Value, cancellationToken);
        }

        product ??= await productRepository.GetByProductCodeAsync(productCode, cancellationToken);

        if (product is not null)
        {
            if (await productRepository.ProductCodeExistsAsync(productCode, product.Id, cancellationToken)
                && !string.Equals(product.ProductCode, productCode, StringComparison.Ordinal))
            {
                AddError(errors, nameof(SaveLatestPriceDto.ProductCode), "كود المنتج مستخدم من قبل.");
                return OperationResult.Failure(errors);
            }

            product.ProductCode = productCode;
            product.NameAr = nameAr;
            product.PurchasePrice = dto.LatestPurchasePrice;
        }
        else
        {
            var categories = await categoryRepository.GetActiveLookupAsync(cancellationToken);

            if (categories.Count == 0)
            {
                AddError(errors, string.Empty, "يجب إنشاء تصنيف واحد على الأقل في النظام أولاً.");
                return OperationResult.Failure(errors);
            }

            product = new Product
            {
                ProductCode = productCode,
                NameAr = nameAr,
                CategoryId = categories[0].Id,
                PurchasePrice = dto.LatestPurchasePrice,
                CurrentAverageCost = 0,
                SellingPrice = 0,
                CurrentStock = 0,
                MinimumStock = 0,
                IsActive = true
            };

            await productRepository.AddAsync(product, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    private async Task<Dictionary<string, List<string>>> ValidateAsync(SaveLatestPriceDto dto, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, List<string>>();
        var productCode = Normalize(dto.ProductCode);
        var nameAr = Normalize(dto.NameAr);

        if (string.IsNullOrWhiteSpace(productCode))
        {
            AddError(errors, nameof(SaveLatestPriceDto.ProductCode), "كود المنتج مطلوب.");
        }

        if (string.IsNullOrWhiteSpace(nameAr))
        {
            AddError(errors, nameof(SaveLatestPriceDto.NameAr), "اسم الصنف مطلوب.");
        }

        if (dto.LatestPurchasePrice <= 0)
        {
            AddError(errors, nameof(SaveLatestPriceDto.LatestPurchasePrice), "أحدث سعر شراء يجب أن يكون أكبر من صفر.");
        }
        else if (dto.LatestPurchasePrice != decimal.Truncate(dto.LatestPurchasePrice))
        {
            AddError(errors, nameof(SaveLatestPriceDto.LatestPurchasePrice), "أحدث سعر شراء يجب أن يكون عدداً صحيحاً.");
        }

        if (dto.Id is > 0 && await productRepository.GetByIdAsync(dto.Id.Value, cancellationToken) is null)
        {
            AddError(errors, string.Empty, "المنتج غير موجود.");
        }

        return errors;
    }

    private static string Normalize(string value)
    {
        return value.Trim().Normalize(NormalizationForm.FormC);
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
