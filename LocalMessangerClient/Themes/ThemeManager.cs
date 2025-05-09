using System;
using System.Windows;

namespace LocalMessangerClient
{
    public enum ThemeType
    {
        Light,
        Dark
    }

    public static class ThemeManager
    {
        private static readonly Uri LightThemeUri = new Uri("Themes/LightTheme.xaml", UriKind.Relative);
        private static readonly Uri DarkThemeUri = new Uri("Themes/DarkTheme.xaml", UriKind.Relative);

        public static ThemeType CurrentTheme { get; private set; } = ThemeType.Light;

        public static void ChangeTheme(ThemeType theme)
        {
            var appDictionaries = Application.Current.Resources.MergedDictionaries;

            ResourceDictionary themeDictionary = null;
            foreach (var dict in appDictionaries)
            {
                if (dict.Source != null &&
                    (dict.Source.OriginalString.EndsWith("LightTheme.xaml") ||
                     dict.Source.OriginalString.EndsWith("DarkTheme.xaml")))
                {
                    themeDictionary = dict;
                    break;
                }
            }

            var newThemeDictionary = new ResourceDictionary();
            if (theme == ThemeType.Dark)
            {
                newThemeDictionary.Source = DarkThemeUri;
                CurrentTheme = ThemeType.Dark;
            }
            else
            {
                newThemeDictionary.Source = LightThemeUri;
                CurrentTheme = ThemeType.Light;
            }

            if (themeDictionary != null)
            {
                int index = appDictionaries.IndexOf(themeDictionary);
                appDictionaries.Remove(themeDictionary);
                appDictionaries.Insert(index, newThemeDictionary);
            }
            else
            {
                appDictionaries.Add(newThemeDictionary);
            }
        }

        public static void ToggleTheme()
        {
            if (CurrentTheme == ThemeType.Light)
                ChangeTheme(ThemeType.Dark);
            else
                ChangeTheme(ThemeType.Light);
        }
    }
}