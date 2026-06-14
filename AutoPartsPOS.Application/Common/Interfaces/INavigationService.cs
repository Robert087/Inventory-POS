using AutoPartsPOS.Application.Common.ViewModels;

namespace AutoPartsPOS.Application.Common.Interfaces;

public interface INavigationService
{
    ViewModelBase? CurrentViewModel { get; }

    Task NavigateToAsync<TViewModel>(CancellationToken cancellationToken = default)
        where TViewModel : ViewModelBase;
}
