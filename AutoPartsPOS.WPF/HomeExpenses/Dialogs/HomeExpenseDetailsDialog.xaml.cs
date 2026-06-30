using AutoPartsPOS.WPF.HomeExpenses.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace AutoPartsPOS.WPF.HomeExpenses.Dialogs;

public partial class HomeExpenseDetailsDialog : Window
{
    public HomeExpenseDetailsDialog()
    {
        InitializeComponent();
        Closing += OnClosing;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is not HomeExpenseDetailsDialogViewModel viewModel)
        {
            return;
        }

        if (DialogResult == true)
        {
            return;
        }

        viewModel.DiscardPendingChanges();
        DialogResult = false;
    }
}
