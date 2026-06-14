using AutoPartsPOS.Application.Catalog.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoPartsPOS.WPF.Purchases.ViewModels;

public sealed partial class PurchaseInvoiceLineViewModel : ObservableObject
{
    [ObservableProperty]
    private ProductDto? _product;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPrice))]
    private decimal _quantity = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPrice))]
    private decimal _unitPrice;

    public decimal TotalPrice => decimal.Round(Quantity * UnitPrice, 2);
}
