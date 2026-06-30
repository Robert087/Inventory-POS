using AutoPartsPOS.WPF.HomeExpenses.Dialogs;
using AutoPartsPOS.WPF.HomeExpenses.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AutoPartsPOS.WPF.HomeExpenses.Services;

public sealed class HomeExpenseDialogService(IServiceProvider serviceProvider) : IHomeExpenseDialogService
{
    public Task<bool> ShowAddDialogAsync(DateOnly? presetDate = null)
    {
        var viewModel = serviceProvider.GetRequiredService<HomeExpenseDialogViewModel>();
        viewModel.Load(presetDate);

        var dialog = new HomeExpenseDialog
        {
            DataContext = viewModel,
            Owner = serviceProvider.GetRequiredService<MainWindow>()
        };

        viewModel.RequestClose += (_, result) => dialog.DialogResult = result;
        return Task.FromResult(dialog.ShowDialog() == true);
    }

    public async Task<bool> ShowDetailsDialogAsync(long dayId)
    {
        var viewModel = serviceProvider.GetRequiredService<HomeExpenseDetailsDialogViewModel>();
        await viewModel.LoadAsync(dayId);

        var dialog = new HomeExpenseDetailsDialog
        {
            DataContext = viewModel,
            Owner = serviceProvider.GetRequiredService<MainWindow>()
        };

        viewModel.RequestClose += (_, result) => dialog.DialogResult = result;
        return dialog.ShowDialog() == true;
    }

    public bool ShowDeleteConfirmationDialog()
    {
        var dialog = new HomeExpenseDeleteConfirmationDialog
        {
            Owner = serviceProvider.GetRequiredService<MainWindow>()
        };

        return dialog.ShowDialog() == true;
    }
}
