using AutoPartsPOS.Application.Catalog.Dtos;
using AutoPartsPOS.WPF.LatestPrices.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace AutoPartsPOS.WPF.LatestPrices.Dialogs;

public partial class LatestPriceDialog : Window
{
    private bool _isUpdatingSelection;

    public LatestPriceDialog()
    {
        InitializeComponent();
    }

    private void CodeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        HandleProductSelection(sender as ComboBox, e);
    }

    private void NameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        HandleProductSelection(sender as ComboBox, e);
    }

    private void HandleProductSelection(ComboBox? comboBox, SelectionChangedEventArgs e)
    {
        if (_isUpdatingSelection
            || comboBox?.SelectedItem is not ProductDto product
            || DataContext is not LatestPriceDialogViewModel viewModel)
        {
            return;
        }

        _isUpdatingSelection = true;

        try
        {
            viewModel.OnProductSelected(product);
            CodeComboBox.Text = product.ProductCode;
            NameComboBox.Text = product.NameAr;
            CodeComboBox.SelectedItem = product;
            NameComboBox.SelectedItem = product;
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }
}
