using AutoPartsPOS.Application.Catalog.Dtos;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AutoPartsPOS.WPF.Catalog.Converters;

public sealed class StockStatusBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ProductDto product)
        {
            return Brushes.Transparent;
        }

        if (product.CurrentStock == 0)
        {
            return Brushes.Firebrick;
        }

        if (product.CurrentStock <= product.MinimumStock)
        {
            return Brushes.Goldenrod;
        }

        return Brushes.ForestGreen;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
