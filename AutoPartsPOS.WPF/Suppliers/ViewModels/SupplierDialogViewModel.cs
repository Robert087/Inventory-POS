using AutoPartsPOS.Application.Suppliers.Dtos;
using AutoPartsPOS.Application.Suppliers.Interfaces;
using AutoPartsPOS.WPF.Catalog.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoPartsPOS.WPF.Suppliers.ViewModels;

public sealed partial class SupplierDialogViewModel(ISupplierService supplierService) : ValidatableDialogViewModel
{
    private long? _supplierId;

    [ObservableProperty]
    private string _dialogTitle = "إضافة مورد";

    [ObservableProperty]
    private string _nameAr = string.Empty;

    [ObservableProperty]
    private string? _phone;

    [ObservableProperty]
    private string? _address;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private bool _isActive = true;

    public void Load(SupplierDto? supplier)
    {
        _supplierId = supplier?.Id;
        DialogTitle = supplier is null ? "إضافة مورد" : "تعديل مورد";
        NameAr = supplier?.NameAr ?? string.Empty;
        Phone = supplier?.Phone;
        Address = supplier?.Address;
        Notes = supplier?.Notes;
        IsActive = supplier?.IsActive ?? true;
        ApplyErrors(new Dictionary<string, string[]>());
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await ExecuteBusyAsync(async cancellationToken =>
        {
            var result = await supplierService.SaveAsync(new SaveSupplierDto
            {
                Id = _supplierId,
                NameAr = NameAr,
                Phone = Phone,
                Address = Address,
                Notes = Notes,
                IsActive = IsActive
            }, cancellationToken);

            if (!result.Succeeded)
            {
                ApplyErrors(result.Errors);
                ErrorMessage = result.ErrorSummary;
                return;
            }

            Close(true);
        });
    }

    [RelayCommand]
    private void Cancel()
    {
        Close(false);
    }
}
