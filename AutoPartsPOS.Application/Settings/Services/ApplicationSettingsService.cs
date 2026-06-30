using AutoPartsPOS.Application.Common.Interfaces;
using AutoPartsPOS.Application.Common.Models;
using AutoPartsPOS.Application.Settings.Dtos;
using AutoPartsPOS.Application.Settings.Interfaces;

namespace AutoPartsPOS.Application.Settings.Services;

public sealed class ApplicationSettingsService(IAppSettingsService appSettingsService) : IApplicationSettingsService
{
    private const string StoreNameKey = "store_name";
    private const string StorePhoneKey = "store_phone";
    private const string StoreAddressKey = "store_address";
    private const string CurrencySymbolKey = "currency_symbol";
    private const string LowStockAlertEnabledKey = "low_stock_alert_enabled";
    private const string SlowMovingDaysKey = "slow_moving_days";
    private const string TopSellingDaysKey = "top_selling_days";

    public async Task<ApplicationSettingsDto> LoadAsync(CancellationToken cancellationToken = default)
    {
        var defaults = CreateDefaults();

        return new ApplicationSettingsDto
        {
            StoreName = await GetStringAsync(StoreNameKey, defaults.StoreName, cancellationToken),
            StorePhone = await GetNullableStringAsync(StorePhoneKey, defaults.StorePhone, cancellationToken),
            StoreAddress = await GetNullableStringAsync(StoreAddressKey, defaults.StoreAddress, cancellationToken),
            CurrencySymbol = await GetStringAsync(CurrencySymbolKey, defaults.CurrencySymbol, cancellationToken),
            LowStockAlertEnabled = await GetBoolAsync(LowStockAlertEnabledKey, defaults.LowStockAlertEnabled, cancellationToken),
            SlowMovingDays = await GetIntAsync(SlowMovingDaysKey, defaults.SlowMovingDays, cancellationToken),
            TopSellingDays = await GetIntAsync(TopSellingDaysKey, defaults.TopSellingDays, cancellationToken)
        };
    }

    public async Task<OperationResult> SaveAsync(ApplicationSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var errors = Validate(settings);

        if (errors.Count > 0)
        {
            return OperationResult.Failure(errors);
        }

        await PersistAsync(settings, cancellationToken);
        return OperationResult.Success();
    }

    public async Task<ApplicationSettingsDto> ResetToDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var defaults = CreateDefaults();
        await PersistAsync(defaults, cancellationToken);
        return defaults;
    }

    private async Task PersistAsync(ApplicationSettingsDto settings, CancellationToken cancellationToken)
    {
        await appSettingsService.SetValueAsync(StoreNameKey, settings.StoreName.Trim(), cancellationToken);
        await appSettingsService.SetValueAsync(StorePhoneKey, NormalizeNullable(settings.StorePhone), cancellationToken);
        await appSettingsService.SetValueAsync(StoreAddressKey, NormalizeNullable(settings.StoreAddress), cancellationToken);
        await appSettingsService.SetValueAsync(CurrencySymbolKey, settings.CurrencySymbol.Trim(), cancellationToken);
        await appSettingsService.SetValueAsync(LowStockAlertEnabledKey, settings.LowStockAlertEnabled.ToString(), cancellationToken);
        await appSettingsService.SetValueAsync(SlowMovingDaysKey, settings.SlowMovingDays.ToString(), cancellationToken);
        await appSettingsService.SetValueAsync(TopSellingDaysKey, settings.TopSellingDays.ToString(), cancellationToken);
    }

    private static Dictionary<string, List<string>> Validate(ApplicationSettingsDto settings)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(settings.StoreName))
        {
            AddError(errors, nameof(ApplicationSettingsDto.StoreName), "اسم المتجر مطلوب.");
        }

        if (string.IsNullOrWhiteSpace(settings.CurrencySymbol))
        {
            AddError(errors, nameof(ApplicationSettingsDto.CurrencySymbol), "رمز العملة مطلوب.");
        }

        if (settings.SlowMovingDays <= 0)
        {
            AddError(errors, nameof(ApplicationSettingsDto.SlowMovingDays), "عدد أيام المنتجات بطيئة الحركة يجب أن يكون أكبر من صفر.");
        }

        if (settings.TopSellingDays <= 0)
        {
            AddError(errors, nameof(ApplicationSettingsDto.TopSellingDays), "عدد أيام المنتجات الأكثر مبيعاً يجب أن يكون أكبر من صفر.");
        }

        return errors;
    }

    private async Task<string> GetStringAsync(string key, string defaultValue, CancellationToken cancellationToken)
    {
        var value = await appSettingsService.GetValueAsync(key, cancellationToken);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private async Task<string?> GetNullableStringAsync(string key, string? defaultValue, CancellationToken cancellationToken)
    {
        var value = await appSettingsService.GetValueAsync(key, cancellationToken);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private async Task<bool> GetBoolAsync(string key, bool defaultValue, CancellationToken cancellationToken)
    {
        var value = await appSettingsService.GetValueAsync(key, cancellationToken);
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private async Task<int> GetIntAsync(string key, int defaultValue, CancellationToken cancellationToken)
    {
        var value = await appSettingsService.GetValueAsync(key, cancellationToken);
        return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : defaultValue;
    }

    private static ApplicationSettingsDto CreateDefaults()
    {
        return new ApplicationSettingsDto
        {
            StoreName = "Taison System",
            StorePhone = string.Empty,
            StoreAddress = string.Empty,
            CurrencySymbol = "ج.م",
            LowStockAlertEnabled = true,
            SlowMovingDays = 90,
            TopSellingDays = 30
        };
    }

    private static string? NormalizeNullable(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static void AddError(Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var messages))
        {
            messages = [];
            errors[key] = messages;
        }

        messages.Add(message);
    }
}
