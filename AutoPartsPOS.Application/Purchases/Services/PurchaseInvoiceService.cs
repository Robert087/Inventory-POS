using AutoPartsPOS.Application.Common.Interfaces;
using AutoPartsPOS.Application.Common.Models;
using AutoPartsPOS.Application.Inventory.Interfaces;
using AutoPartsPOS.Application.Purchases.Dtos;
using AutoPartsPOS.Application.Purchases.Interfaces;
using AutoPartsPOS.Application.Suppliers.Interfaces;
using AutoPartsPOS.Domain.Inventory;
using AutoPartsPOS.Domain.Purchases;
using System.Text;

namespace AutoPartsPOS.Application.Purchases.Services;

public sealed class PurchaseInvoiceService(
    IPurchaseInvoiceRepository purchaseInvoiceRepository,
    IInventoryRepository inventoryRepository,
    ISupplierRepository supplierRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork) : IPurchaseInvoiceService
{
    public Task<IReadOnlyList<PurchaseInvoiceListDto>> SearchAsync(string? searchText, CancellationToken cancellationToken = default)
    {
        return purchaseInvoiceRepository.SearchAsync(searchText, cancellationToken);
    }

    public Task<PurchaseInvoiceDetailsDto?> GetDetailsAsync(long id, CancellationToken cancellationToken = default)
    {
        return purchaseInvoiceRepository.GetDetailsAsync(id, cancellationToken);
    }

    public async Task<OperationResult> CreateAsync(CreatePurchaseInvoiceDto dto, CancellationToken cancellationToken = default)
    {
        var errors = await ValidateCreateAsync(dto, cancellationToken);

        if (errors.Count > 0)
        {
            return OperationResult.Failure(errors);
        }

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var invoice = new PurchaseInvoice
            {
                InvoiceNumber = Normalize(dto.InvoiceNumber),
                SupplierId = dto.SupplierId,
                InvoiceDate = dto.InvoiceDate,
                Notes = NormalizeNullable(dto.Notes),
                Status = PurchaseInvoiceStatus.Posted,
                Items = dto.Items.Select(item => new PurchaseInvoiceItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = decimal.Round(item.Quantity * item.UnitPrice, 2)
                }).ToList()
            };

            invoice.TotalAmount = invoice.Items.Sum(item => item.TotalPrice);

            await purchaseInvoiceRepository.AddAsync(invoice, token);
            await unitOfWork.SaveChangesAsync(token);

            foreach (var item in invoice.Items)
            {
                var product = await inventoryRepository.GetProductForUpdateAsync(item.ProductId, token)
                    ?? throw new InvalidOperationException("Product disappeared while posting purchase invoice.");

                product.CurrentStock += item.Quantity;

                await inventoryRepository.AddTransactionAsync(new InventoryTransaction
                {
                    ProductId = item.ProductId,
                    TransactionType = InventoryTransactionType.Purchase,
                    Quantity = item.Quantity,
                    BalanceAfter = product.CurrentStock,
                    ReferenceType = InventoryReferenceType.PurchaseInvoice,
                    ReferenceId = invoice.Id,
                    TransactionDate = dateTimeProvider.Now,
                    Notes = invoice.InvoiceNumber
                }, token);
            }

            await unitOfWork.SaveChangesAsync(token);
        }, cancellationToken);

        return OperationResult.Success();
    }

    public async Task<OperationResult> VoidAsync(long id, string? reason = null, CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, List<string>>();
        var invoice = await purchaseInvoiceRepository.GetByIdWithItemsAsync(id, cancellationToken);

        if (invoice is null)
        {
            AddError(errors, string.Empty, "فاتورة الشراء غير موجودة.");
            return OperationResult.Failure(errors);
        }

        if (invoice.Status == PurchaseInvoiceStatus.Voided)
        {
            AddError(errors, nameof(PurchaseInvoice.Status), "فاتورة الشراء ملغاة بالفعل.");
            return OperationResult.Failure(errors);
        }

        foreach (var item in invoice.Items)
        {
            var product = await inventoryRepository.GetProductForUpdateAsync(item.ProductId, cancellationToken);

            if (product is null)
            {
                AddError(errors, string.Empty, "أحد منتجات الفاتورة غير موجود.");
                continue;
            }

            if (product.CurrentStock < item.Quantity)
            {
                AddError(errors, string.Empty, $"لا يمكن إلغاء الفاتورة لأن مخزون المنتج {product.NameAr} سيصبح أقل من صفر.");
            }
        }

        if (errors.Count > 0)
        {
            return OperationResult.Failure(errors);
        }

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var currentInvoice = await purchaseInvoiceRepository.GetByIdWithItemsAsync(id, token)
                ?? throw new InvalidOperationException("Purchase invoice disappeared while voiding.");

            currentInvoice.Status = PurchaseInvoiceStatus.Voided;
            currentInvoice.VoidedAt = dateTimeProvider.Now;
            currentInvoice.VoidReason = NormalizeNullable(reason);

            foreach (var item in currentInvoice.Items)
            {
                var product = await inventoryRepository.GetProductForUpdateAsync(item.ProductId, token)
                    ?? throw new InvalidOperationException("Product disappeared while voiding purchase invoice.");

                if (product.CurrentStock < item.Quantity)
                {
                    throw new InvalidOperationException("Voiding purchase invoice would create negative stock.");
                }

                product.CurrentStock -= item.Quantity;

                await inventoryRepository.AddTransactionAsync(new InventoryTransaction
                {
                    ProductId = item.ProductId,
                    TransactionType = InventoryTransactionType.VoidPurchase,
                    Quantity = -item.Quantity,
                    BalanceAfter = product.CurrentStock,
                    ReferenceType = InventoryReferenceType.PurchaseInvoice,
                    ReferenceId = currentInvoice.Id,
                    TransactionDate = dateTimeProvider.Now,
                    Notes = currentInvoice.InvoiceNumber
                }, token);
            }

            await unitOfWork.SaveChangesAsync(token);
        }, cancellationToken);

        return OperationResult.Success();
    }

    private async Task<Dictionary<string, List<string>>> ValidateCreateAsync(CreatePurchaseInvoiceDto dto, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, List<string>>();
        var invoiceNumber = Normalize(dto.InvoiceNumber);

        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            AddError(errors, nameof(CreatePurchaseInvoiceDto.InvoiceNumber), "رقم الفاتورة مطلوب.");
        }
        else if (await purchaseInvoiceRepository.InvoiceNumberExistsAsync(invoiceNumber, cancellationToken))
        {
            AddError(errors, nameof(CreatePurchaseInvoiceDto.InvoiceNumber), "رقم الفاتورة مستخدم من قبل.");
        }

        if (dto.SupplierId <= 0 || await supplierRepository.GetByIdAsync(dto.SupplierId, cancellationToken) is null)
        {
            AddError(errors, nameof(CreatePurchaseInvoiceDto.SupplierId), "يجب اختيار مورد صحيح.");
        }

        if (dto.Items.Count == 0)
        {
            AddError(errors, nameof(CreatePurchaseInvoiceDto.Items), "يجب إضافة منتج واحد على الأقل.");
        }

        for (var index = 0; index < dto.Items.Count; index++)
        {
            var item = dto.Items[index];

            if (item.ProductId <= 0 || await inventoryRepository.GetProductForUpdateAsync(item.ProductId, cancellationToken) is null)
            {
                AddError(errors, $"{nameof(CreatePurchaseInvoiceDto.Items)}[{index}].{nameof(CreatePurchaseInvoiceItemDto.ProductId)}", "يجب اختيار منتج صحيح.");
            }

            if (item.Quantity <= 0)
            {
                AddError(errors, $"{nameof(CreatePurchaseInvoiceDto.Items)}[{index}].{nameof(CreatePurchaseInvoiceItemDto.Quantity)}", "الكمية يجب أن تكون أكبر من صفر.");
            }

            if (item.UnitPrice < 0)
            {
                AddError(errors, $"{nameof(CreatePurchaseInvoiceDto.Items)}[{index}].{nameof(CreatePurchaseInvoiceItemDto.UnitPrice)}", "سعر الوحدة لا يمكن أن يكون أقل من صفر.");
            }
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
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized.Normalize(NormalizationForm.FormC);
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
