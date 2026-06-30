using System.Windows;

namespace AutoPartsPOS.WPF.HomeExpenses.Dialogs;

public partial class HomeExpenseDeleteConfirmationDialog : Window
{
    public HomeExpenseDeleteConfirmationDialog()
    {
        InitializeComponent();
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
