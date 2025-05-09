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
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Start the main window
            var mainWindow = new LocalMessanger.MainWindow();
            mainWindow.Show();
        }
    }
}