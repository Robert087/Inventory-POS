using AutoPartsPOS.Application.Common.Interfaces;
using AutoPartsPOS.Application.Common.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoPartsPOS.WPF.Services;

public sealed partial class NavigationService(
    IServiceScopeFactory scopeFactory,
    ILogger<NavigationService> logger) : ObservableObject, INavigationService, IDisposable
{
    private IServiceScope? _currentScope;

    [ObservableProperty]
    private ViewModelBase? _currentViewModel;

    public async Task NavigateToAsync<TViewModel>(CancellationToken cancellationToken = default)
        where TViewModel : ViewModelBase
    {
        var nextScope = scopeFactory.CreateScope();
        var viewModel = nextScope.ServiceProvider.GetRequiredService<TViewModel>();

        try
        {
            await viewModel.InitializeAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            nextScope.Dispose();
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to initialize navigation target {ViewModelType}", typeof(TViewModel).Name);
            viewModel.ErrorMessage = "تعذر تحميل بيانات الشاشة. يرجى التحقق من اتصال قاعدة البيانات ثم إعادة المحاولة.";
        }

        var previousScope = _currentScope;
        _currentScope = nextScope;
        CurrentViewModel = viewModel;
        previousScope?.Dispose();
    }

    public void Dispose()
    {
        _currentScope?.Dispose();
        _currentScope = null;
    }
}
