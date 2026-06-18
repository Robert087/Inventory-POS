using System.Windows;
using System.Windows.Input;

namespace AutoPartsPOS.WPF.Purchases.Dialogs;

public partial class PurchaseInvoiceDialog : Window
{
    public PurchaseInvoiceDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        InvoiceNumberTextBox.Focus();
        InvoiceNumberTextBox.SelectAll();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter &&
            (Keyboard.FocusedElement == ProductComboBox ||
             Keyboard.FocusedElement == QuantityTextBox ||
             Keyboard.FocusedElement == UnitPriceTextBox))
        {
            if (AddLineButton.Command?.CanExecute(null) == true)
            {
                AddLineButton.Command.Execute(null);
                ProductComboBox.Focus();
            }

            e.Handled = true;
            return;
        }

        if (e.Key == Key.Delete && Keyboard.FocusedElement == LinesGrid)
        {
            if (DataContext is Purchases.ViewModels.PurchaseInvoiceDialogViewModel viewModel &&
                viewModel.RemoveLineCommand.CanExecute(null))
            {
                viewModel.RemoveLineCommand.Execute(null);
            }

            e.Handled = true;
        }
    }
}
