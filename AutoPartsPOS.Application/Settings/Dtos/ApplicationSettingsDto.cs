namespace AutoPartsPOS.Application.Settings.Dtos;

public sealed class ApplicationSettingsDto
{
    public string StoreName { get; init; } = string.Empty;

    public string? StorePhone { get; init; }

    public string? StoreAddress { get; init; }

    public string CurrencySymbol { get; init; } = "ج.م";

    public bool LowStockAlertEnabled { get; init; } = true;

    public int SlowMovingDays { get; init; } = 90;

    public int TopSellingDays { get; init; } = 30;
}
