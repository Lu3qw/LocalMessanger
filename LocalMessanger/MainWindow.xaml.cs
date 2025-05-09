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
    private AuthService authService;


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

    public MainWindow()
    {
        InitializeComponent();
        logService = new LogService(db, log => AppendLog(log));
        chatServer = new ChatServer(port, logService);
        authService = new AuthService(db, logService); // Initialize AuthService instance

        this.DataContext = this;
    }

    private void UnbanButton_Click(object sender, RoutedEventArgs e)
    {
        var bannedUsers = db.BannedUsers.Select(b => b.Username).ToList();

        if (bannedUsers.Count == 0)
        {
            MessageBox.Show("There are no banned users.", "Unban User");
            return;
        }

        var userSelectDialog = new UserSelectWindow("Unban User", bannedUsers)
        {
            Owner = this
        };

        if (userSelectDialog.ShowDialog() == true)
        {
            string username = userSelectDialog.SelectedUsername;
            if (!string.IsNullOrEmpty(username))
            {
                authService.UnbanUser(username); 
                MessageBox.Show($"{username} has been unbanned.", "Unban");
                logService.Log($"User '{username}' has been unbanned", "Info");
            }
        }
    }

    private void BroadcastMessageToAll(string message)
    {
        foreach (var userWriter in chatServer.GetAllUserWriters())
        {
            try
            {
                userWriter.Value.WriteLineAsync($"broadcast:SERVER:{message}");
            }
            catch (Exception ex)
            {
                logService.Log($"Error sending broadcast to a client: {ex.Message}", "Error");
            }
        }

        logService.Log($"Broadcast message sent: {message}", "Info");
    }

    private void BanButton_Click(object sender, RoutedEventArgs e)
    {

        var userSelectDialog = new UserSelectWindow("Ban User", db.Users.Select(u => u.Username).ToList())
        {
            Owner = this
        };

        if (userSelectDialog.ShowDialog() == true)
        {
            string username = userSelectDialog.SelectedUsername;
            if (!string.IsNullOrEmpty(username))
            {
                authService.BanUser(username);
                // Disconnect the user if they're connected
                chatServer.DisconnectUser(username);
                MessageBox.Show($"{username} has been banned.", "Ban");
                logService.Log($"User '{username}' has been banned", "Warning");
            }
        }
    }

    private void UnbanButton_Click(object sender, RoutedEventArgs e)
    {
        var bannedUsers = db.BannedUsers.Select(b => b.Username).ToList();

        if (bannedUsers.Count == 0)
        {
            MessageBox.Show("There are no banned users.", "Unban User");
            return;
        }

        var userSelectDialog = new UserSelectWindow("Unban User", bannedUsers)
        {
            Owner = this
        };

        if (userSelectDialog.ShowDialog() == true)
        {
            string username = userSelectDialog.SelectedUsername;
            if (!string.IsNullOrEmpty(username))
            {
                authService.UnbanUser(username);
                MessageBox.Show($"{username} has been unbanned.", "Unban");
                logService.Log($"User '{username}' has been unbanned", "Info");
            }
        }
    }

    private void OpenDatabaseButton_Click(object sender, RoutedEventArgs e)
    {
        var dbWindow = new DatabaseWindow();
        dbWindow.Show();
    }
}
