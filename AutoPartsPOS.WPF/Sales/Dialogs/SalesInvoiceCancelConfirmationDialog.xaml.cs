using System.Windows;

namespace AutoPartsPOS.WPF.Sales.Dialogs;

public partial class SalesInvoiceCancelConfirmationDialog : Window
{
    public SalesInvoiceCancelConfirmationDialog()
    {
        InitializeComponent();
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
