using AutoPartsPOS.Application.Common.Interfaces;
using AutoPartsPOS.Application.Common.ViewModels;
using AutoPartsPOS.WPF.Catalog.ViewModels;
using AutoPartsPOS.WPF.Inventory.ViewModels;
using AutoPartsPOS.WPF.Purchases.ViewModels;
using AutoPartsPOS.WPF.Reports.ViewModels;
using AutoPartsPOS.WPF.Sales.ViewModels;
using AutoPartsPOS.WPF.Settings.ViewModels;
using AutoPartsPOS.WPF.Suppliers.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Threading;

namespace AutoPartsPOS.WPF.ViewModels;

public sealed partial class ShellViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ViewModelBase? _currentViewModel;

    [ObservableProperty]
    private string _activePage = "Dashboard";

    [ObservableProperty]
    private bool _isSidebarExpanded = true;

    [ObservableProperty]
    private double _sidebarWidth = 252;

    [ObservableProperty]
    private string _currentDateTime = string.Empty;

    public ShellViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        Title = "نظام إدارة إكسسوارات السيارات";

        if (_navigationService is INotifyPropertyChanged observableNavigation)
        {
            observableNavigation.PropertyChanged += OnNavigationPropertyChanged;
        }

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) => UpdateDateTime();
        timer.Start();
        UpdateDateTime();
    }

    public override Task InitializeAsync(CancellationToken cancellationToken = default) =>
        _navigationService.NavigateToAsync<HomeViewModel>(cancellationToken);

    [RelayCommand]
    private Task NavigateHomeAsync() => NavigateAsync<HomeViewModel>("Dashboard");

    [RelayCommand]
    private Task NavigateCategoriesAsync() => NavigateAsync<CategoriesViewModel>("Categories");

    [RelayCommand]
    private Task NavigateProductsAsync() => NavigateAsync<ProductsViewModel>("Products");

    [RelayCommand]
    private Task NavigateSuppliersAsync() => NavigateAsync<SuppliersViewModel>("Suppliers");

    [RelayCommand]
    private Task NavigatePurchasesAsync() => NavigateAsync<PurchaseInvoicesViewModel>("Purchases");

    [RelayCommand]
    private Task NavigateSalesAsync() => NavigateAsync<SalesInvoicesViewModel>("Sales");

    [RelayCommand]
    private Task NavigateInventoryLedgerAsync() => NavigateAsync<InventoryLedgerViewModel>("Inventory");

    [RelayCommand]
    private Task NavigateReportsAsync() => NavigateAsync<ReportsViewModel>("Reports");

    [RelayCommand]
    private Task NavigateSettingsAsync() => NavigateAsync<AppSettingsViewModel>("Settings");

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
        SidebarWidth = IsSidebarExpanded ? 252 : 84;
    }

    private async Task NavigateAsync<TViewModel>(string page)
        where TViewModel : ViewModelBase
    {
        ActivePage = page;
        await _navigationService.NavigateToAsync<TViewModel>();
    }

    private void UpdateDateTime() =>
        CurrentDateTime = DateTime.Now.ToString("dddd، dd MMMM yyyy  •  hh:mm tt", new CultureInfo("ar-EG"));

    private void OnNavigationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(INavigationService.CurrentViewModel))
        {
            CurrentViewModel = _navigationService.CurrentViewModel;
        }
    }
}
