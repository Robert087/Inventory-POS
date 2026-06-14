using AutoPartsPOS.Application.Catalog.Dtos;

namespace AutoPartsPOS.WPF.Catalog.Services;

public interface ICatalogDialogService
{
    Task<bool> ShowCategoryDialogAsync(CategoryDto? category);

    Task<bool> ShowProductDialogAsync(ProductDto? product);
}
