using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System;

namespace DiceDetector.View.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var visible = value is bool b && b;
            var invert = parameter?.ToString()?.Contains("Invert", StringComparison.OrdinalIgnoreCase) == true;
            if (invert)
            {
                visible = !visible;
            }

            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}