using System.Windows;
using BCrypt.Net;
namespace LocalMessangerClient;

public partial class LoginWindow : Window
{
    private ChatClient? _chatClient;

    public LoginWindow()
    {
        InitializeComponent();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (_chatClient != null)
        {
            _chatClient.Disconnect();
            _chatClient = null;
        }

        try
        {
            _chatClient = new ChatClient("127.0.0.1", 25001);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during connecting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var username = UsernameTextBox.Text.Trim();
        var password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Username and password cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var storedHash = await _chatClient.SendRequestAsync($"get_salt:{username}");
            if (storedHash.StartsWith("error:"))
            {
                MessageBox.Show("User not found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(password, storedHash);
            var response = await _chatClient.SendAuthRequestAsync("login", username, hash);
            if (response == "success")
            {
                MessageBox.Show("Login successful!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);

                var mw = new MainWindow(_chatClient, username);
                Application.Current.MainWindow = mw;
                this.Closing -= Window_Closing;
                mw.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Invalid username or password.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)    
        {
            MessageBox.Show($"Error during login: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private async void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        if (_chatClient != null)
        {
            _chatClient.Disconnect();
            _chatClient = null;
        }

        try
        {
            _chatClient = new ChatClient("127.0.0.1", 25001);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during connecting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var username = UsernameTextBox.Text.Trim();
        var password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Username and password cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(password);

        try
        {
            var response = await _chatClient.SendAuthRequestAsync("register", username, hash);
            if (response == "success")
                MessageBox.Show("Registration successful!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("Username already exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during registration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _chatClient?.Disconnect();
    }
}