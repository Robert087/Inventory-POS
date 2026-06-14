using AutoPartsPOS.Application.Common.Interfaces;
using AutoPartsPOS.Application.Common.Models;
using AutoPartsPOS.Application.Inventory.Interfaces;
using AutoPartsPOS.Application.Sales.Dtos;
using AutoPartsPOS.Application.Sales.Interfaces;
using AutoPartsPOS.Domain.Inventory;
using AutoPartsPOS.Domain.Sales;
using System.Text;

namespace AutoPartsPOS.Application.Sales.Services;

public sealed class SalesInvoiceService(
    ISalesInvoiceRepository salesInvoiceRepository,
    IInventoryRepository inventoryRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork) : ISalesInvoiceService
{
    public Task<IReadOnlyList<SalesInvoiceListDto>> SearchAsync(string? searchText, CancellationToken cancellationToken = default)
    {
        return salesInvoiceRepository.SearchAsync(searchText, cancellationToken);
    }

    public Task<SalesInvoiceDetailsDto?> GetDetailsAsync(long id, CancellationToken cancellationToken = default)
    {
        return salesInvoiceRepository.GetDetailsAsync(id, cancellationToken);
    }

    public async Task<OperationResult> CreateAsync(CreateSalesInvoiceDto dto, CancellationToken cancellationToken = default)
    {
        var errors = await ValidateCreateAsync(dto, cancellationToken);

        if (errors.Count > 0)
        {
            return OperationResult.Failure(errors);
        }

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            foreach (var groupedItem in dto.Items.GroupBy(item => item.ProductId))
            {
                var product = await inventoryRepository.GetProductForUpdateAsync(groupedItem.Key, token)
                    ?? throw new InvalidOperationException("Product disappeared while posting sales invoice.");
                var requestedQuantity = groupedItem.Sum(item => item.Quantity);

                if (product.CurrentStock < requestedQuantity)
                {
                    throw new InvalidOperationException($"Cannot sell more than available stock for product {product.NameAr}.");
                }
            }

            var invoice = new SalesInvoice
            {
                InvoiceNumber = Normalize(dto.InvoiceNumber),
                InvoiceDate = dto.InvoiceDate,
                Notes = NormalizeNullable(dto.Notes),
                Status = SalesInvoiceStatus.Posted,
                DiscountAmount = dto.DiscountAmount,
                Items = dto.Items.Select(item => new SalesInvoiceItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = decimal.Round(item.Quantity * item.UnitPrice, 2)
                }).ToList()
            };

            invoice.SubtotalAmount = invoice.Items.Sum(item => item.TotalPrice);
            invoice.NetTotalAmount = invoice.SubtotalAmount - invoice.DiscountAmount;

            await salesInvoiceRepository.AddAsync(invoice, token);
            await unitOfWork.SaveChangesAsync(token);

            foreach (var item in invoice.Items)
            {
                var product = await inventoryRepository.GetProductForUpdateAsync(item.ProductId, token)
                    ?? throw new InvalidOperationException("Product disappeared while posting sales invoice.");

                if (product.CurrentStock < item.Quantity)
                {
                    throw new InvalidOperationException("Posting sales invoice would create negative stock.");
                }

                product.CurrentStock -= item.Quantity;

                await inventoryRepository.AddTransactionAsync(new InventoryTransaction
                {
                    ProductId = item.ProductId,
                    TransactionType = InventoryTransactionType.Sale,
                    Quantity = -item.Quantity,
                    BalanceAfter = product.CurrentStock,
                    ReferenceType = InventoryReferenceType.SalesInvoice,
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
        var invoice = await salesInvoiceRepository.GetByIdWithItemsAsync(id, cancellationToken);

        if (invoice is null)
        {
            AddError(errors, string.Empty, "فاتورة البيع غير موجودة.");
            return OperationResult.Failure(errors);
        }

        if (invoice.Status == SalesInvoiceStatus.Voided)
        {
            AddError(errors, nameof(SalesInvoice.Status), "فاتورة البيع ملغاة بالفعل.");
            return OperationResult.Failure(errors);
        }

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var currentInvoice = await salesInvoiceRepository.GetByIdWithItemsAsync(id, token)
                ?? throw new InvalidOperationException("Sales invoice disappeared while voiding.");

            if (currentInvoice.Status == SalesInvoiceStatus.Voided)
            {
                throw new InvalidOperationException("Sales invoice is already voided.");
            }

            currentInvoice.Status = SalesInvoiceStatus.Voided;
            currentInvoice.VoidedAt = dateTimeProvider.Now;
            currentInvoice.VoidReason = NormalizeNullable(reason);

            foreach (var item in currentInvoice.Items)
            {
                var product = await inventoryRepository.GetProductForUpdateAsync(item.ProductId, token)
                    ?? throw new InvalidOperationException("Product disappeared while voiding sales invoice.");

                product.CurrentStock += item.Quantity;

                await inventoryRepository.AddTransactionAsync(new InventoryTransaction
                {
                    ProductId = item.ProductId,
                    TransactionType = InventoryTransactionType.VoidSale,
                    Quantity = item.Quantity,
                    BalanceAfter = product.CurrentStock,
                    ReferenceType = InventoryReferenceType.SalesInvoice,
                    ReferenceId = currentInvoice.Id,
                    TransactionDate = dateTimeProvider.Now,
                    Notes = currentInvoice.InvoiceNumber
                }, token);
            }

            await unitOfWork.SaveChangesAsync(token);
        }, cancellationToken);

        return OperationResult.Success();
    }

    private async Task<Dictionary<string, List<string>>> ValidateCreateAsync(CreateSalesInvoiceDto dto, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, List<string>>();
        var invoiceNumber = Normalize(dto.InvoiceNumber);

        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            AddError(errors, nameof(CreateSalesInvoiceDto.InvoiceNumber), "رقم الفاتورة مطلوب.");
        }
        else if (await salesInvoiceRepository.InvoiceNumberExistsAsync(invoiceNumber, cancellationToken))
        {
            AddError(errors, nameof(CreateSalesInvoiceDto.InvoiceNumber), "رقم الفاتورة مستخدم من قبل.");
        }

        if (dto.Items.Count == 0)
        {
            AddError(errors, nameof(CreateSalesInvoiceDto.Items), "يجب إضافة منتج واحد على الأقل.");
        }

        if (dto.DiscountAmount < 0)
        {
            AddError(errors, nameof(CreateSalesInvoiceDto.DiscountAmount), "الخصم لا يمكن أن يكون أقل من صفر.");
        }

        var subtotal = dto.Items.Sum(item => decimal.Round(item.Quantity * item.UnitPrice, 2));

        if (dto.DiscountAmount > subtotal)
        {
            AddError(errors, nameof(CreateSalesInvoiceDto.DiscountAmount), "الخصم لا يمكن أن يكون أكبر من إجمالي الفاتورة.");
        }

        for (var index = 0; index < dto.Items.Count; index++)
        {
            var item = dto.Items[index];
            var product = item.ProductId > 0
                ? await inventoryRepository.GetProductForUpdateAsync(item.ProductId, cancellationToken)
                : null;

            if (product is null)
            {
                AddError(errors, $"{nameof(CreateSalesInvoiceDto.Items)}[{index}].{nameof(CreateSalesInvoiceItemDto.ProductId)}", "يجب اختيار منتج صحيح.");
            }

            if (item.Quantity <= 0)
            {
                AddError(errors, $"{nameof(CreateSalesInvoiceDto.Items)}[{index}].{nameof(CreateSalesInvoiceItemDto.Quantity)}", "الكمية يجب أن تكون أكبر من صفر.");
            }

            if (item.UnitPrice < 0)
            {
                AddError(errors, $"{nameof(CreateSalesInvoiceDto.Items)}[{index}].{nameof(CreateSalesInvoiceItemDto.UnitPrice)}", "سعر الوحدة لا يمكن أن يكون أقل من صفر.");
            }
        }

        foreach (var groupedItem in dto.Items.GroupBy(item => item.ProductId))
        {
            var product = await inventoryRepository.GetProductForUpdateAsync(groupedItem.Key, cancellationToken);

            if (product is null)
            {
                continue;
            }

            var requestedQuantity = groupedItem.Sum(item => item.Quantity);

            if (product.CurrentStock < requestedQuantity)
            {
                AddError(errors, nameof(CreateSalesInvoiceDto.Items), $"لا يمكن بيع كمية أكبر من المتاح للمنتج {product.NameAr}. المتاح: {product.CurrentStock:N3}.");
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
