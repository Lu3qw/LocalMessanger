using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LocalMessangerClient
{
    public class BoolToAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? HorizontalAlignment.Right : HorizontalAlignment.Left;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}