using AutoPartsPOS.Application.Common.Interfaces;
using AutoPartsPOS.Application.Common.ViewModels;
using AutoPartsPOS.Application.Settings.Interfaces;
using AutoPartsPOS.WPF.Catalog.ViewModels;
using AutoPartsPOS.WPF.Backups.ViewModels;
using AutoPartsPOS.WPF.Inventory.ViewModels;
using AutoPartsPOS.WPF.LatestPrices.ViewModels;
using AutoPartsPOS.WPF.HomeExpenses.ViewModels;
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
using Microsoft.Extensions.DependencyInjection;

namespace AutoPartsPOS.WPF.ViewModels;

public sealed partial class ShellViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IServiceScopeFactory _scopeFactory;

    [ObservableProperty]
    private ViewModelBase? _currentViewModel;

    [ObservableProperty]
    private string _activePage = "Dashboard";

    [ObservableProperty]
    private bool _isSidebarExpanded = true;

    [ObservableProperty]
    private double _sidebarWidth = 308;

    [ObservableProperty]
    private string _currentDateTime = string.Empty;

    [ObservableProperty]
    private bool _isOperationsExpanded = true;

    [ObservableProperty]
    private bool _isInventoryExpanded = true;

    public ShellViewModel(INavigationService navigationService, IServiceScopeFactory scopeFactory)
    {
        _navigationService = navigationService;
        _scopeFactory = scopeFactory;
        Title = "Taison System";

        if (_navigationService is INotifyPropertyChanged observableNavigation)
        {
            observableNavigation.PropertyChanged += OnNavigationPropertyChanged;
        }

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) => UpdateDateTime();
        timer.Start();
        UpdateDateTime();
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var settingsService = scope.ServiceProvider.GetRequiredService<IApplicationSettingsService>();
        var settings = await settingsService.LoadAsync(cancellationToken);
        ApplyStoreName(settings.StoreName);

        await _navigationService.NavigateToAsync<HomeViewModel>(cancellationToken);
    }

    public void ApplyStoreName(string storeName)
    {
        if (!string.IsNullOrWhiteSpace(storeName))
        {
            Title = storeName.Trim();
        }
    }

    [RelayCommand]
    private Task NavigateHomeAsync() => NavigateAsync<HomeViewModel>("Dashboard");

    [RelayCommand]
    private Task NavigateCategoriesAsync() => NavigateAsync<CategoriesViewModel>("Categories");

    [RelayCommand]
    private Task NavigateProductsAsync() => NavigateAsync<ProductsViewModel>("Products");

    [RelayCommand]
    private Task NavigateLatestPricesAsync() => NavigateAsync<LatestPricesViewModel>("LatestPrices");

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
    private Task NavigateHomeExpensesAsync() => NavigateAsync<HomeExpensesViewModel>("HomeExpenses");

    [RelayCommand]
    private Task NavigateSettingsAsync() => NavigateAsync<AppSettingsViewModel>("Settings");

    [RelayCommand]
    private Task NavigateBackupsAsync() => NavigateAsync<BackupsViewModel>("Backups");

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
        SidebarWidth = IsSidebarExpanded ? 308 : 92;
    }

    private async Task NavigateAsync<TViewModel>(string page)
        where TViewModel : ViewModelBase
    {
        ActivePage = page;
        UpdateNavigationGroups(page);
        await _navigationService.NavigateToAsync<TViewModel>();
    }

    private void UpdateNavigationGroups(string page)
    {
        if (!IsSidebarExpanded)
        {
            return;
        }

        if (page is "Purchases" or "Sales")
        {
            IsOperationsExpanded = true;
        }

        if (page is "Products" or "Categories" or "Inventory" or "LatestPrices")
        {
            IsInventoryExpanded = true;
        }
    }

    private void UpdateDateTime()
    {
        CurrentDateTime = DateTime.Now.ToString("dddd، dd MMMM yyyy - hh:mm tt", new CultureInfo("ar-EG"));
    }

    private void OnNavigationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(INavigationService.CurrentViewModel))
        {
            CurrentViewModel = _navigationService.CurrentViewModel;
        }
    }
}
