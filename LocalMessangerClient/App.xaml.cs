using System.Windows;

namespace LocalMessangerClient;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Застосовуємо базову тему Light (або буде замінена на ту, що збережена в реєстрі)
        ThemeManager.ChangeTheme(ThemeType.Light);
    }
}