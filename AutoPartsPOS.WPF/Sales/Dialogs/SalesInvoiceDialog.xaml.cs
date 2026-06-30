using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace AutoPartsPOS.WPF.Sales.Dialogs;

public partial class SalesInvoiceDialog : Window
{
    public SalesInvoiceDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ProductComboBox.Focus();
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
            if (DataContext is Sales.ViewModels.SalesInvoiceDialogViewModel viewModel &&
                viewModel.RemoveLineCommand.CanExecute(null))
            {
                viewModel.RemoveLineCommand.Execute(null);
            }

            e.Handled = true;
        }
    }

    private void WholeNumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = e.Text.Any(character => character is < '0' or > '9');
    }

    private void WholeNumberTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText) ||
            e.SourceDataObject.GetData(DataFormats.UnicodeText) is not string text ||
            text.Any(character => character is < '0' or > '9'))
        {
            e.CancelCommand();
        }
    }
}
