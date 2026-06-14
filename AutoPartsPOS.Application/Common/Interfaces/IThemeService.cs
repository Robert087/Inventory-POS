using AutoPartsPOS.Application.Common.Models;

namespace AutoPartsPOS.Application.Common.Interfaces;

public interface IThemeService
{
    AppTheme CurrentTheme { get; }

    void Apply(AppTheme theme);
}
