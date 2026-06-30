using System.Windows;
using System.Windows.Input;

namespace AutoPartsPOS.WPF.Sales.Dialogs;

public partial class SalesInvoiceDetailsDialog : Window
{
    public SalesInvoiceDetailsDialog()
    {
        InitializeComponent();
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
