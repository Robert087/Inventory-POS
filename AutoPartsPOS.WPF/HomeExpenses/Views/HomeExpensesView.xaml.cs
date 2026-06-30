using AutoPartsPOS.WPF.HomeExpenses.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace AutoPartsPOS.WPF.HomeExpenses.Views;

public partial class HomeExpensesView : UserControl
{
    public HomeExpensesView()
    {
        InitializeComponent();
    }

    private void ExpenseDaysGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is HomeExpensesViewModel viewModel && viewModel.ViewCommand.CanExecute(null))
        {
            viewModel.ViewCommand.Execute(null);
        }
    }
}
