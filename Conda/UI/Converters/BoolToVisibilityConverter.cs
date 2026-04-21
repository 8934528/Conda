using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Conda.UI.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (parameter is string param && param == "Invert")
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                if (parameter is string param && param == "Invert")
                    return visibility != Visibility.Visible;
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}
