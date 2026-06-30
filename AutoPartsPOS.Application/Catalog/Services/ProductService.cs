using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.Application.Catalog.Interfaces;
using AutoPartsPOS.Application.Common.Interfaces;
using AutoPartsPOS.Application.Common.Models;
using AutoPartsPOS.Application.Inventory.Interfaces;
using AutoPartsPOS.Application.Inventory.Services;
using AutoPartsPOS.Domain.Catalog;
using AutoPartsPOS.Domain.Inventory;
using System.Text;

namespace AutoPartsPOS.Application.Catalog.Services;

public sealed class ProductService(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IInventoryRepository inventoryRepository,
    IDateTimeProvider dateTimeProvider,
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

        return MapToDto(product);
    }

    public async Task<ProductDto?> GetByProductCodeAsync(string productCode, CancellationToken cancellationToken = default)
    {
        var normalizedCode = Normalize(productCode);

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return null;
        }

        var product = await productRepository.GetByProductCodeAsync(normalizedCode, cancellationToken);

        return product is null ? null : MapToDto(product);
    }

    public async Task<OperationResult> ReplenishStockAsync(ReplenishProductStockDto dto, CancellationToken cancellationToken = default)
    {
        var errors = ValidateReplenishment(dto);

        if (errors.Count > 0)
        {
            return OperationResult.Failure(errors);
        }

        var productExists = await productRepository.GetByIdAsync(dto.ProductId, cancellationToken);

        if (productExists is null)
        {
            AddError(errors, string.Empty, "المنتج غير موجود.");
            return OperationResult.Failure(errors);
        }

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var product = await inventoryRepository.GetProductForUpdateAsync(dto.ProductId, token)
                ?? throw new InvalidOperationException("Product disappeared while replenishing stock.");

            var oldStock = product.CurrentStock;
            product.CurrentAverageCost = InventoryCostCalculator.CalculateWeightedAverageCost(
                oldStock,
                product.CurrentAverageCost,
                dto.Quantity,
                dto.PurchasePrice);
            product.PurchasePrice = dto.PurchasePrice;
            product.CurrentStock += dto.Quantity;

            await inventoryRepository.AddTransactionAsync(new InventoryTransaction
            {
                ProductId = dto.ProductId,
                TransactionType = InventoryTransactionType.Adjustment,
                Quantity = dto.Quantity,
                BalanceAfter = product.CurrentStock,
                ReferenceType = InventoryReferenceType.ManualAdjustment,
                ReferenceId = dto.ProductId,
                TransactionDate = dateTimeProvider.Now,
                Notes = "إضافة كمية للصنف"
            }, token);

            await unitOfWork.SaveChangesAsync(token);
        }, cancellationToken);

        return OperationResult.Success();
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
        else if (dto.PurchasePrice != decimal.Truncate(dto.PurchasePrice))
        {
            AddError(errors, nameof(SaveProductDto.PurchasePrice), "سعر الشراء يجب أن يكون عدداً صحيحاً.");
        }

        if (dto.SellingPrice < 0)
        {
            AddError(errors, nameof(SaveProductDto.SellingPrice), "سعر البيع لا يمكن أن يكون أقل من صفر.");
        }
        else if (dto.SellingPrice != decimal.Truncate(dto.SellingPrice))
        {
            AddError(errors, nameof(SaveProductDto.SellingPrice), "سعر البيع يجب أن يكون عدداً صحيحاً.");
        }

        if (dto.CurrentStock < 0)
        {
            AddError(errors, nameof(SaveProductDto.CurrentStock), "المخزون الحالي لا يمكن أن يكون أقل من صفر.");
        }
        else if (dto.CurrentStock != decimal.Truncate(dto.CurrentStock))
        {
            AddError(errors, nameof(SaveProductDto.CurrentStock), "المخزون يجب أن يكون عدداً صحيحاً.");
        }

        if (dto.MinimumStock < 0)
        {
            AddError(errors, nameof(SaveProductDto.MinimumStock), "الحد الأدنى للمخزون لا يمكن أن يكون أقل من صفر.");
        }
        else if (dto.MinimumStock != decimal.Truncate(dto.MinimumStock))
        {
            AddError(errors, nameof(SaveProductDto.MinimumStock), "الحد الأدنى يجب أن يكون عدداً صحيحاً.");
        }

        return errors;
    }

    private static Dictionary<string, List<string>> ValidateReplenishment(ReplenishProductStockDto dto)
    {
        var errors = new Dictionary<string, List<string>>();

        if (dto.ProductId <= 0)
        {
            AddError(errors, nameof(ReplenishProductStockDto.ProductId), "المنتج غير موجود.");
        }

        if (dto.Quantity <= 0)
        {
            AddError(errors, nameof(ReplenishProductStockDto.Quantity), "الكمية الجديدة يجب أن تكون أكبر من صفر.");
        }
        else if (dto.Quantity != decimal.Truncate(dto.Quantity))
        {
            AddError(errors, nameof(ReplenishProductStockDto.Quantity), "الكمية الجديدة يجب أن تكون عدداً صحيحاً.");
        }

        if (dto.PurchasePrice <= 0)
        {
            AddError(errors, nameof(ReplenishProductStockDto.PurchasePrice), "سعر الشراء الجديد يجب أن يكون أكبر من صفر.");
        }
        else if (dto.PurchasePrice != decimal.Truncate(dto.PurchasePrice))
        {
            AddError(errors, nameof(ReplenishProductStockDto.PurchasePrice), "سعر الشراء الجديد يجب أن يكون عدداً صحيحاً.");
        }

        return errors;
    }

    private static ProductDto MapToDto(Product product)
    {
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
