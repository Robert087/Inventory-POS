using AutoPartsPOS.Application.Common.Interfaces;
using AutoPartsPOS.Application.Common.Models;
using MaterialDesignThemes.Wpf;

namespace AutoPartsPOS.WPF.Services;

public sealed class ThemeService : IThemeService
{
    private readonly PaletteHelper _paletteHelper = new();

    public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    public void Apply(AppTheme theme)
    {
        var materialTheme = _paletteHelper.GetTheme();
        materialTheme.SetBaseTheme(theme == AppTheme.Dark ? BaseTheme.Dark : BaseTheme.Light);
        _paletteHelper.SetTheme(materialTheme);
        CurrentTheme = theme;
    }
}
