using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.Application.Catalog.Interfaces;
using AutoPartsPOS.Application.Common.ViewModels;
using AutoPartsPOS.WPF.Catalog.Services;
using AutoPartsPOS.WPF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AutoPartsPOS.WPF.Catalog.ViewModels;

public sealed partial class CategoriesViewModel(
    ICategoryService categoryService,
    ICatalogDialogService dialogService,
    IDeleteConfirmationService deleteConfirmationService) : ViewModelBase
{
    public ObservableCollection<CategoryDto> Categories { get; } = [];

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeactivateCommand))]
    private CategoryDto? _selectedCategory;

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Title = "التصنيفات";
        await LoadAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (await dialogService.ShowCategoryDialogAsync(null))
        {
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task EditRowAsync(CategoryDto? category)
    {
        if (category is not null && await dialogService.ShowCategoryDialogAsync(category))
        {
            await LoadAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedCategory))]
    private async Task DeactivateAsync()
    {
        if (SelectedCategory is null)
        {
            return;
        }

        await ExecuteBusyAsync(async cancellationToken =>
        {
            var result = await categoryService.DeactivateAsync(SelectedCategory.Id, cancellationToken);

            if (!result.Succeeded)
            {
                ErrorMessage = result.ErrorSummary;
                return;
            }

            await LoadAsync(cancellationToken);
        });
    }

    [RelayCommand]
    private async Task DeleteRowAsync(CategoryDto? category)
    {
        if (category is null || !deleteConfirmationService.Confirm("التصنيف", category.NameAr))
        {
            return;
        }

        await ExecuteBusyAsync(async cancellationToken =>
        {
            var result = await categoryService.DeleteAsync(category.Id, cancellationToken);
            if (!result.Succeeded)
            {
                ErrorMessage = result.ErrorSummary;
                return;
            }

            Categories.Remove(category);
            if (SelectedCategory?.Id == category.Id)
            {
                SelectedCategory = null;
            }
        });
    }

    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await ExecuteBusyAsync(async token =>
        {
            Categories.Clear();

            foreach (var category in await categoryService.SearchAsync(SearchText, token))
            {
                Categories.Add(category);
            }
        }, cancellationToken);
    }

    private bool HasSelectedCategory()
    {
        return SelectedCategory is not null;
    }
}
