using LocalMessangerServer;
using LocalMessangerServer.EF;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace LocalMessanger;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    AppDbContext db = new AppDbContext();
    LogService logService;
    ChatServer chatServer;

    int port = 25001;
    public ObservableCollection<string> ConnectedUsers => chatServer.ConnectedUsers;

    private string _serverLog = string.Empty;
    public string ServerLog
    {
        get => _serverLog;
        set
        {
            _serverLog = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void AppendLog(string log)
    {
        Dispatcher.Invoke(() =>
        {
            ServerLog += log + Environment.NewLine;
        });
    }

    public MainWindow()
    {
        InitializeComponent();
        logService = new LogService(db, log => AppendLog(log));
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
}
