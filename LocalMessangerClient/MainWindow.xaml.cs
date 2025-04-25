using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace LocalMessangerClient;

public partial class MainWindow : Window
{
    private ChatClient? _chatClient;
    private readonly string _username;

    public MainWindow(ChatClient chatClient, string username)
    {
        InitializeComponent();

        _chatClient = chatClient;
        _chatClient.MessageReceived += OnMessageReceived;
        _username = username;
    }

    private void OnMessageReceived(string message)
    {
        Dispatcher.Invoke(() =>
        {
            ChatHistoryPanel.Children.Add(new TextBlock { Text = message, Margin = new Thickness(5) });
        });
    }

    private async void SendMessageButton_Click(object sender, RoutedEventArgs e)
    {
        if (_chatClient != null && !string.IsNullOrWhiteSpace(MessageTextBox.Text) && !string.IsNullOrWhiteSpace(ReceiverTextBox.Text))
        {
            var receiver = ReceiverTextBox.Text.Trim();
            var content = MessageTextBox.Text.Trim();

            try
            {
                await _chatClient.SendMessageAsync(_username, receiver, content); // Використовуємо _username як відправника
                MessageTextBox.Clear();
            }
            catch
            {
                MessageBox.Show("Failed to send the message.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            MessageBox.Show("Please enter a message and a receiver.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Window_Closed(object sender, System.EventArgs e)
    {
        _chatClient?.Disconnect();
    }
}
