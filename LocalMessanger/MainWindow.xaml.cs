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
    private readonly AppDbContext _db;
    private readonly LogService _logService;
    private readonly ChatServer _chatServer;
    private readonly AuthService _authService;
    private readonly int _port = 25001;
    private CancellationTokenSource _cts;

    public ObservableCollection<string> ConnectedUsers => _chatServer.ConnectedUsers;

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
        _db = new AppDbContext();
        _logService = new LogService(_db, log => AppendLog(log));
        _chatServer = new ChatServer(_port, _logService);
        _authService = new AuthService(_db, _logService);

        this.DataContext = this;

        // Disable buttons until server is started
        SetButtonsEnabled(false);
    }

    private void SetButtonsEnabled(bool enabled)
    {
        BroadcastButton.IsEnabled = enabled;
        BanButton.IsEnabled = enabled;
        UnbanButton.IsEnabled = enabled;
        OpenDatabaseButton.IsEnabled = enabled;
    }

    private void StartStopServerButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.Content.ToString() == "Start Server")
        {
            button.Content = "Stop Server";
            _cts = new CancellationTokenSource();

            _chatServer.StartAsync(_cts.Token).ContinueWith(task =>
            {
                if (task.IsFaulted && task.Exception != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _logService.Log($"Error starting server: {task.Exception.InnerException?.Message ?? task.Exception.Message}", "Error");
                        button.Content = "Start Server";
                    });
                }
            });

            SetButtonsEnabled(true);
        }
        else
        {
            button.Content = "Start Server";
            _cts?.Cancel();
            _chatServer.Stop();
            SetButtonsEnabled(false);
        }
    }

    private void BroadcastButton_Click(object sender, RoutedEventArgs e)
    {
        var broadcastWindow = new BroadcastWindow
        {
            Owner = this
        };

        if (broadcastWindow.ShowDialog() == true)
        {
            string message = broadcastWindow.MessageText;
            if (!string.IsNullOrWhiteSpace(message))
            {
                _chatServer.BroadcastToAll(message);
            }
        }
    }

    private void BanButton_Click(object sender, RoutedEventArgs e)
    {
        var usernames = _db.Users.Select(u => u.Username).ToList();

        if (usernames.Count == 0)
        {
            MessageBox.Show("There are no users to ban.", "Ban User");
            return;
        }

        var userSelectDialog = new UserSelectWindow("Ban User", usernames)
        {
            Owner = this
        };

        if (userSelectDialog.ShowDialog() == true)
        {
            string username = userSelectDialog.SelectedUsername;
            if (!string.IsNullOrEmpty(username))
            {
                _authService.BanUser(username);
                // Disconnect the user if they're connected
                _chatServer.DisconnectUser(username);
                MessageBox.Show($"{username} has been banned.", "Ban");
                _logService.Log($"User '{username}' has been banned", "Warning");
            }
        }
    }

    private void UnbanButton_Click(object sender, RoutedEventArgs e)
    {
        var bannedUsers = _db.BannedUsers.Select(b => b.Username).ToList();

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
                _authService.UnbanUser(username);
                MessageBox.Show($"{username} has been unbanned.", "Unban");
                _logService.Log($"User '{username}' has been unbanned", "Info");
            }
        }
    }

    private void OpenDatabaseButton_Click(object sender, RoutedEventArgs e)
    {
        var dbWindow = new DatabaseWindow();
        dbWindow.Show();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _cts?.Cancel();
        _chatServer.Stop();
        _db.Dispose();
        base.OnClosing(e);
    }

    private void ChangeThemeButton_Click(object sender, RoutedEventArgs e)
    {

    }
}