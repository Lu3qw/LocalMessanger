using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LocalMessanger;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void StartStopServerButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;


        if (button.Content.ToString() == "Start Server")
        {
            button.Content = "Stop Server";

        }
        else
        {
            button.Content = "Start Server";

        }

    }

    private void Button_MouseEnter(object sender, MouseEventArgs e)
    {

    }

    private void Button_MouseLeave(object sender, MouseEventArgs e)
    {

    }
}