using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.WPF.Catalog.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace AutoPartsPOS.WPF.Catalog.Dialogs;

public partial class StockReplenishmentDialog : Window
{
    private bool _isUpdatingSelection;

    public StockReplenishmentDialog()
    {
        InitializeComponent();
    }

    private void NameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        HandleProductSelection(sender as ComboBox);
    }

    private void CodeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        HandleProductSelection(sender as ComboBox);
    }

    private void HandleProductSelection(ComboBox? comboBox)
    {
        if (_isUpdatingSelection
            || comboBox?.SelectedItem is not ProductDto product
            || DataContext is not StockReplenishmentDialogViewModel viewModel)
        {
            return;
        }

        _isUpdatingSelection = true;

        try
        {
            viewModel.OnProductSelected(product);
            NameComboBox.Text = product.NameAr;
            CodeComboBox.Text = product.ProductCode;
            NameComboBox.SelectedItem = product;
            CodeComboBox.SelectedItem = product;
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }
}
