using AutoPartsPOS.Application.Common.Interfaces;
using AutoPartsPOS.Application.Common.Models;
using AutoPartsPOS.Application.Inventory.Interfaces;
using AutoPartsPOS.Application.Sales.Dtos;
using AutoPartsPOS.Application.Sales.Interfaces;
using AutoPartsPOS.Domain.Inventory;
using AutoPartsPOS.Domain.Common;
using AutoPartsPOS.Domain.Sales;
using System.Text;

namespace AutoPartsPOS.Application.Sales.Services;

public sealed class SalesInvoiceService(
    ISalesInvoiceRepository salesInvoiceRepository,
    IInventoryRepository inventoryRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork) : ISalesInvoiceService
{
    public Task<IReadOnlyList<SalesInvoiceListDto>> SearchAsync(
        string? searchText,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        return salesInvoiceRepository.SearchAsync(searchText, fromDate, toDate, cancellationToken);
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
            var costSnapshots = new Dictionary<long, decimal>();

            foreach (var groupedItem in dto.Items.GroupBy(item => item.ProductId))
            {
                var product = await inventoryRepository.GetProductForUpdateAsync(groupedItem.Key, token)
                    ?? throw new InvalidOperationException("Product disappeared while posting sales invoice.");
                var requestedQuantity = groupedItem.Sum(item => item.Quantity);

                if (product.CurrentStock < requestedQuantity)
                {
                    throw new InvalidOperationException($"Cannot sell more than available stock for product {product.NameAr}.");
                }

                costSnapshots[product.Id] = product.CurrentAverageCost;
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
                    TotalPrice = decimal.Round(item.Quantity * item.UnitPrice, 2),
                    UnitCost = costSnapshots[item.ProductId],
                    TotalCost = decimal.Round(item.Quantity * costSnapshots[item.ProductId], 2)
                }).ToList()
            };

            invoice.SubtotalAmount = invoice.Items.Sum(item => item.TotalPrice);
            invoice.NetTotalAmount = invoice.SubtotalAmount - invoice.DiscountAmount;
            var payment = CalculatePayment(dto.PaymentStatus!.Value, invoice.NetTotalAmount, dto.PaidAmount);
            invoice.PaymentStatus = dto.PaymentStatus.Value;
            invoice.PaidAmount = payment.PaidAmount;
            invoice.RemainingAmount = payment.RemainingAmount;

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

    public async Task<OperationResult> UpdatePaymentAsync(
        long id,
        InvoicePaymentStatus paymentStatus,
        decimal paidAmount,
        CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, List<string>>();
        var invoice = await salesInvoiceRepository.GetByIdWithItemsAsync(id, cancellationToken);

        if (invoice is null)
        {
            AddError(errors, string.Empty, "فاتورة البيع غير موجودة.");
        }
        else if (invoice.Status == SalesInvoiceStatus.Voided)
        {
            AddError(errors, nameof(SalesInvoice.Status), "لا يمكن تعديل دفعات فاتورة ملغاة.");
        }
        else
        {
            ValidatePayment(paymentStatus, paidAmount, invoice.NetTotalAmount, errors);
        }

        if (errors.Count > 0)
        {
            return OperationResult.Failure(errors);
        }

        var payment = CalculatePayment(paymentStatus, invoice!.NetTotalAmount, paidAmount);
        invoice.PaymentStatus = paymentStatus;
        invoice.PaidAmount = payment.PaidAmount;
        invoice.RemainingAmount = payment.RemainingAmount;
        await unitOfWork.SaveChangesAsync(cancellationToken);
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

        if (dto.PaymentStatus is null || !Enum.IsDefined(dto.PaymentStatus.Value))
        {
            AddError(errors, nameof(CreateSalesInvoiceDto.PaymentStatus), "يجب اختيار حالة الدفع.");
        }

        if (dto.DiscountAmount < 0)
        {
            AddError(errors, nameof(CreateSalesInvoiceDto.DiscountAmount), "الخصم لا يمكن أن يكون أقل من صفر.");
        }
        else if (dto.DiscountAmount != decimal.Truncate(dto.DiscountAmount))
        {
            AddError(errors, nameof(CreateSalesInvoiceDto.DiscountAmount), "الخصم يجب أن يكون عدداً صحيحاً.");
        }

        var subtotal = dto.Items.Sum(item => decimal.Round(item.Quantity * item.UnitPrice, 2));

        if (dto.DiscountAmount > subtotal)
        {
            AddError(errors, nameof(CreateSalesInvoiceDto.DiscountAmount), "الخصم لا يمكن أن يكون أكبر من إجمالي الفاتورة.");
        }

        if (dto.PaymentStatus is not null)
        {
            ValidatePayment(dto.PaymentStatus.Value, dto.PaidAmount, subtotal - dto.DiscountAmount, errors);
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
            else if (item.Quantity != decimal.Truncate(item.Quantity))
            {
                AddError(errors, $"{nameof(CreateSalesInvoiceDto.Items)}[{index}].{nameof(CreateSalesInvoiceItemDto.Quantity)}", "الكمية يجب أن تكون عدداً صحيحاً.");
            }

            if (item.UnitPrice < 0)
            {
                AddError(errors, $"{nameof(CreateSalesInvoiceDto.Items)}[{index}].{nameof(CreateSalesInvoiceItemDto.UnitPrice)}", "سعر الوحدة لا يمكن أن يكون أقل من صفر.");
            }
            else if (item.UnitPrice != decimal.Truncate(item.UnitPrice))
            {
                AddError(errors, $"{nameof(CreateSalesInvoiceDto.Items)}[{index}].{nameof(CreateSalesInvoiceItemDto.UnitPrice)}", "سعر الوحدة يجب أن يكون عدداً صحيحاً.");
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
                AddError(errors, nameof(CreateSalesInvoiceDto.Items), $"لا يمكن بيع كمية أكبر من المتاح للمنتج {product.NameAr}. المتاح: {product.CurrentStock:F0}.");
            }
        }

        return errors;
    }

    private static void ValidatePayment(
        InvoicePaymentStatus paymentStatus,
        decimal paidAmount,
        decimal netTotal,
        Dictionary<string, List<string>> errors)
    {
        if (!Enum.IsDefined(paymentStatus))
        {
            AddError(errors, nameof(CreateSalesInvoiceDto.PaymentStatus), "حالة الدفع غير صحيحة.");
            return;
        }

        if (paidAmount < 0 || paidAmount != decimal.Truncate(paidAmount))
        {
            AddError(errors, nameof(CreateSalesInvoiceDto.PaidAmount), "المبلغ المدفوع يجب أن يكون عدداً صحيحاً غير سالب.");
        }

        if (paymentStatus == InvoicePaymentStatus.PartiallyPaid && (paidAmount <= 0 || paidAmount >= netTotal))
        {
            AddError(errors, nameof(CreateSalesInvoiceDto.PaidAmount), "الدفع الجزئي يجب أن يكون أكبر من صفر وأقل من صافي الفاتورة.");
        }
    }

    private static (decimal PaidAmount, decimal RemainingAmount) CalculatePayment(
        InvoicePaymentStatus paymentStatus,
        decimal netTotal,
        decimal requestedPaidAmount)
    {
        return paymentStatus switch
        {
            InvoicePaymentStatus.Paid => (netTotal, 0),
            InvoicePaymentStatus.Unpaid => (0, netTotal),
            InvoicePaymentStatus.PartiallyPaid => (requestedPaidAmount, netTotal - requestedPaidAmount),
            _ => throw new ArgumentOutOfRangeException(nameof(paymentStatus))
        };
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
