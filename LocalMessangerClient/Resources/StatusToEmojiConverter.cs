using System;
using System.Globalization;
using System.Windows.Data;

namespace LocalMessangerClient
{
    public class StatusToEmojiConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UserStatus status)
            {
                return status switch
                {
                    UserStatus.Online => "🟢",
                    UserStatus.Away => "🟡",
                    UserStatus.DoNotDisturb => "🔴",
                    UserStatus.Offline => "⚫",
                    _ => string.Empty
                };
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}