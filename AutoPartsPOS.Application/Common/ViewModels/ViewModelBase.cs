using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoPartsPOS.Application.Common.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public virtual Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected async Task ExecuteBusyAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await operation(cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
