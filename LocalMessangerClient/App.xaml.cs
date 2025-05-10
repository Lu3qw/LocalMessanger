using System.Windows;
using Microsoft.Win32;
using System;

namespace LocalMessangerClient;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private const string RegistryKeyPath = @"Software\LocalMessanger";
    private const string RegistryValueName = "SelectedTheme";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);


        ThemeType themeToApply = LoadThemeFromRegistry();
        ThemeManager.ChangeTheme(themeToApply);
    }

    private ThemeType LoadThemeFromRegistry()
    {
        try
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            string? themeStr = key?.GetValue(RegistryValueName) as string;
            key?.Close();

            if (!string.IsNullOrEmpty(themeStr) && Enum.TryParse(themeStr, out ThemeType theme))
            {
                return theme;
            }
        }
        catch (Exception)
        {
            
        }

        return ThemeType.Light; 
    }
}