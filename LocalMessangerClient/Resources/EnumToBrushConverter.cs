using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using LocalMessangerClient;

namespace LocalMessangerClient
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (UserStatus)value switch
            {
                UserStatus.Online => Brushes.LightGreen,
                UserStatus.Offline => Brushes.LightGray,
                UserStatus.DoNotDisturb => Brushes.IndianRed,
                UserStatus.Away => Brushes.Khaki,
                _ => Brushes.Transparent
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
