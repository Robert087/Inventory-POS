using AutoPartsPOS.Application.Suppliers.Dtos;

namespace AutoPartsPOS.WPF.Suppliers.Services;

public interface ISupplierDialogService
{
    Task<bool> ShowSupplierDialogAsync(SupplierDto? supplier);
}
