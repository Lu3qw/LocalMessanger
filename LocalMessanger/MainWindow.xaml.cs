using LocalMessangerServer;
using LocalMessangerServer.EF;
using System.Collections.ObjectModel;
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
    AppDbContext db = new AppDbContext();
    LogService logService;
    ChatServer chatServer;
    int port = 25001;

    public ObservableCollection<string> ConnectedUsers => chatServer.ConnectedUsers;

    public MainWindow()
    {
        InitializeComponent();
        logService = new LogService(db);
        chatServer = new ChatServer(port, logService);

        this.DataContext = this;
    }

    private void StartStopServerButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.Content.ToString() == "Start Server")
        {
            button.Content = "Stop Server";

            chatServer.StartAsync(new CancellationToken()).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    logService.Log($"Error starting server: {task.Exception?.Message}", "Error");
                }
            });
        }
        else
        {
            button.Content = "Start Server";
            chatServer.Stop();
        }
    }

    private void Button_MouseEnter(object sender, MouseEventArgs e)
    {

    }

    private void Button_MouseLeave(object sender, MouseEventArgs e)
    {

    }

    private void BroadcastButton_Click(object sender, RoutedEventArgs e)
    {
      

    }
}