using Microsoft.Win32;
using System;
using System.Windows;
using System.Linq;
using System.Windows.Media;

namespace LocalMessangerClient
{
    public enum ThemeType
    {
        Light,
        Dark
    }

    public static class ThemeManager
    {
        private const string RegistryKeyPath = @"Software\LocalMessanger";
        private const string RegistryValueName = "SelectedTheme";
        private static readonly Uri ThemeDictionaryUri = new Uri("Themes/ThemeDictionary.xaml", UriKind.Relative);
        private static readonly Uri LightThemeUri = new Uri("Themes/LightTheme.xaml", UriKind.Relative);
        private static readonly Uri DarkThemeUri = new Uri("Themes/DarkTheme.xaml", UriKind.Relative);

        public static ThemeType CurrentTheme { get; private set; } = ThemeType.Light;

        public static void ChangeTheme(ThemeType theme)
        {

            if (Application.Current == null)
                return;

            var appDictionaries = Application.Current.Resources.MergedDictionaries;


            ResourceDictionary? themeDictionary = appDictionaries.FirstOrDefault(d =>
                d.Source != null &&
                (d.Source.OriginalString.EndsWith("LightTheme.xaml") ||
                 d.Source.OriginalString.EndsWith("DarkTheme.xaml")));

            ResourceDictionary? baseThemeDictionary = appDictionaries.FirstOrDefault(d =>
                d.Source != null &&
                d.Source.OriginalString.EndsWith("ThemeDictionary.xaml"));


            if (baseThemeDictionary == null)
            {
                baseThemeDictionary = new ResourceDictionary { Source = ThemeDictionaryUri };
                appDictionaries.Add(baseThemeDictionary);
            }


            if (themeDictionary != null)
                appDictionaries.Remove(themeDictionary);

            ResourceDictionary newThemeDictionary;
            if (theme == ThemeType.Dark)
            {
                newThemeDictionary = new ResourceDictionary { Source = DarkThemeUri };
                CurrentTheme = ThemeType.Dark;
            }
            else
            {
                newThemeDictionary = new ResourceDictionary { Source = LightThemeUri };
                CurrentTheme = ThemeType.Light;
            }

            appDictionaries.Add(newThemeDictionary);

   
            SaveThemeToRegistry(theme.ToString());
        }

        private static void SaveThemeToRegistry(string themeName)
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
                key?.SetValue(RegistryValueName, themeName);
                key?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка збереження теми в реєстрі: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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