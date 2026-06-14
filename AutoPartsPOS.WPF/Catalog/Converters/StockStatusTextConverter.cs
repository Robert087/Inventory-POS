using AutoPartsPOS.Application.Catalog.Dtos;
using System.Globalization;
using System.Windows.Data;

namespace AutoPartsPOS.WPF.Catalog.Converters;

public sealed class StockStatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ProductDto product)
        {
            return string.Empty;
        }

        if (product.CurrentStock == 0)
        {
            return "نفد المخزون";
        }

        if (product.CurrentStock <= product.MinimumStock)
        {
            return "منخفض";
        }

        return "متوفر";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
