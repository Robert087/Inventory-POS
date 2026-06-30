using AutoPartsPOS.Application.Common.ViewModels;
using AutoPartsPOS.Application.Settings.Dtos;
using AutoPartsPOS.Application.Settings.Interfaces;
using AutoPartsPOS.WPF.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AutoPartsPOS.WPF.Settings.ViewModels;

public sealed partial class AppSettingsViewModel(
    IApplicationSettingsService settingsService,
    ShellViewModel shellViewModel) : ViewModelBase, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = [];

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public ObservableCollection<string> SaveMessages { get; } = [];

    [ObservableProperty]
    private string _storeName = string.Empty;

    [ObservableProperty]
    private string? _storePhone;

    [ObservableProperty]
    private string? _storeAddress;

    [ObservableProperty]
    private string _currencySymbol = "ج.م";

    [ObservableProperty]
    private bool _lowStockAlertEnabled = true;

    [ObservableProperty]
    private int _slowMovingDays = 90;

    [ObservableProperty]
    private int _topSellingDays = 30;

    public bool HasErrors => _errors.Count > 0;

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Title = "الإعدادات";
        await LoadAsync(cancellationToken);
    }

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return _errors.SelectMany(error => error.Value);
        }

        return _errors.TryGetValue(propertyName, out var errors)
            ? errors
            : Array.Empty<string>();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await ExecuteBusyAsync(async cancellationToken =>
        {
            SaveMessages.Clear();
            var result = await settingsService.SaveAsync(CreateDto(), cancellationToken);

            if (!result.Succeeded)
            {
                ApplyErrors(result.Errors);
                ErrorMessage = result.ErrorSummary;
                return;
            }

            ApplyErrors(new Dictionary<string, string[]>());
            StoreName = StoreName.Trim();
            CurrencySymbol = CurrencySymbol.Trim();
            shellViewModel.ApplyStoreName(StoreName);
            SaveMessages.Add("تم حفظ الإعدادات بنجاح.");
        });
    }

    [RelayCommand]
    private async Task ResetAsync()
    {
        await ExecuteBusyAsync(async cancellationToken =>
        {
            SaveMessages.Clear();
            var defaults = await settingsService.ResetToDefaultsAsync(cancellationToken);
            ApplySettings(defaults);
            shellViewModel.ApplyStoreName(defaults.StoreName);
            ApplyErrors(new Dictionary<string, string[]>());
            SaveMessages.Add("تمت استعادة الإعدادات الافتراضية.");
        });
    }

    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await ExecuteBusyAsync(async token =>
        {
            SaveMessages.Clear();
            var settings = await settingsService.LoadAsync(token);
            ApplySettings(settings);
            shellViewModel.ApplyStoreName(settings.StoreName);
        }, cancellationToken);
    }

    private ApplicationSettingsDto CreateDto()
    {
        return new ApplicationSettingsDto
        {
            StoreName = StoreName,
            StorePhone = StorePhone,
            StoreAddress = StoreAddress,
            CurrencySymbol = CurrencySymbol,
            LowStockAlertEnabled = LowStockAlertEnabled,
            SlowMovingDays = SlowMovingDays,
            TopSellingDays = TopSellingDays
        };
    }

    private void ApplySettings(ApplicationSettingsDto settings)
    {
        StoreName = settings.StoreName;
        StorePhone = settings.StorePhone;
        StoreAddress = settings.StoreAddress;
        CurrencySymbol = settings.CurrencySymbol;
        LowStockAlertEnabled = settings.LowStockAlertEnabled;
        SlowMovingDays = settings.SlowMovingDays;
        TopSellingDays = settings.TopSellingDays;
        ErrorMessage = null;
    }

    private void ApplyErrors(IReadOnlyDictionary<string, string[]> errors)
    {
        var changedProperties = _errors.Keys.Union(errors.Keys).Distinct().ToArray();
        _errors.Clear();

        foreach (var error in errors)
        {
            _errors[error.Key] = [.. error.Value];
        }

        foreach (var propertyName in changedProperties)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        OnPropertyChanged(nameof(HasErrors));
    }
}
