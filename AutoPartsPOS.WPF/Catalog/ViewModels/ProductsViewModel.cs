using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.Application.Catalog.Interfaces;
using AutoPartsPOS.Application.Common.ViewModels;
using AutoPartsPOS.WPF.Catalog.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AutoPartsPOS.WPF.Catalog.ViewModels;

public sealed partial class ProductsViewModel(
    IProductService productService,
    ICategoryService categoryService,
    ICatalogDialogService dialogService) : ViewModelBase
{
    public ObservableCollection<ProductDto> Products { get; } = [];

    public ObservableCollection<CategoryLookupDto> Categories { get; } = [];

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private CategoryLookupDto? _selectedCategory;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeactivateCommand))]
    private ProductDto? _selectedProduct;

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Title = "الأصناف";
        await LoadCategoriesAsync(cancellationToken);
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
        if (await dialogService.ShowProductDialogAsync(null))
        {
            await LoadCategoriesAsync();
            await LoadAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedProduct))]
    private async Task EditAsync()
    {
        if (SelectedProduct is not null && await dialogService.ShowProductDialogAsync(SelectedProduct))
        {
            await LoadCategoriesAsync();
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task ReplenishStockAsync()
    {
        ErrorMessage = null;

        if (await dialogService.ShowStockReplenishmentDialogAsync())
        {
            await LoadAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedProduct))]
    private async Task DeactivateAsync()
    {
        if (SelectedProduct is null)
        {
            return;
        }

        await ExecuteBusyAsync(async cancellationToken =>
        {
            var result = await productService.DeactivateAsync(SelectedProduct.Id, cancellationToken);

            if (!result.Succeeded)
            {
                ErrorMessage = result.ErrorSummary;
                return;
            }

            await LoadAsync(cancellationToken);
        });
    }

    private async Task LoadCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var selectedId = SelectedCategory?.Id ?? 0;
        Categories.Clear();
        Categories.Add(new CategoryLookupDto(0, "كل التصنيفات"));

        foreach (var category in await categoryService.GetActiveLookupAsync(cancellationToken))
        {
            Categories.Add(category);
        }

        SelectedCategory = Categories.FirstOrDefault(category => category.Id == selectedId) ?? Categories.FirstOrDefault();
    }

    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await ExecuteBusyAsync(async token =>
        {
            Products.Clear();
            long? categoryId = SelectedCategory?.Id > 0 ? SelectedCategory.Id : null;

            foreach (var product in await productService.SearchAsync(SearchText, categoryId, token))
            {
                Products.Add(product);
            }
        }, cancellationToken);
    }

    private bool HasSelectedProduct()
    {
        return SelectedProduct is not null;
    }
}
