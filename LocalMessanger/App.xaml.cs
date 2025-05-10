using LocalMessangerClient;
using Microsoft.Win32;
using System;
using System.Windows;

namespace LocalMessangerServer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // Initialize database if needed
            using (var db = new EF.AppDbContext())
            {
                try
                {
                    db.Database.EnsureCreated();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Database initialization error: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Load theme from registry
            LoadThemeFromRegistry();
        }

        private void LoadThemeFromRegistry()
        {
            try
            {
                const string registryKeyPath = @"Software\LocalMessanger";
                const string registryValueName = "SelectedTheme";

                RegistryKey? key = Registry.CurrentUser.OpenSubKey(registryKeyPath);
                if (key != null)
                {
                    string? themeName = key.GetValue(registryValueName) as string;
                    key.Close();

                    if (!string.IsNullOrEmpty(themeName) && Enum.TryParse<ThemeType>(themeName, out ThemeType theme))
                    {
                        ThemeManager.ChangeTheme(theme);
                    }
                    else
                    {

                        ThemeManager.ChangeTheme(ThemeType.Light);
                    }
                }
                else
                {
                    ThemeManager.ChangeTheme(ThemeType.Light);
                }
            }
            catch (Exception)
            {
                ThemeManager.ChangeTheme(ThemeType.Light);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new LocalMessanger.MainWindow();
            mainWindow.Show();
        }
    }
}