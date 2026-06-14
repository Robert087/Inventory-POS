using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.Application.Catalog.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoPartsPOS.WPF.Catalog.ViewModels;

public sealed partial class CategoryDialogViewModel(ICategoryService categoryService) : ValidatableDialogViewModel
{
    private long? _categoryId;

    [ObservableProperty]
    private string _dialogTitle = "إضافة تصنيف";

    [ObservableProperty]
    private string _nameAr = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private bool _isActive = true;

    public void Load(CategoryDto? category)
    {
        _categoryId = category?.Id;
        DialogTitle = category is null ? "إضافة تصنيف" : "تعديل تصنيف";
        NameAr = category?.NameAr ?? string.Empty;
        Description = category?.Description;
        IsActive = category?.IsActive ?? true;
        ApplyErrors(new Dictionary<string, string[]>());
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await ExecuteBusyAsync(async cancellationToken =>
        {
            var result = await categoryService.SaveAsync(new SaveCategoryDto
            {
                Id = _categoryId,
                NameAr = NameAr,
                Description = Description,
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
