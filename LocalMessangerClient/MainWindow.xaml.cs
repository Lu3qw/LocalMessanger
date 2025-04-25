using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace LocalMessangerClient;

public partial class MainWindow : Window
{
    private ChatClient? _chatClient;
    private readonly string _username;

    // The constructor now receives the already-connected ChatClient and the username.
    public MainWindow(ChatClient chatClient, string username)
    {
        InitializeComponent();
        _chatClient = chatClient;
        _username = username;

        // Update header TextBlock with the logged in username.
        UsernameDisplayTextBlock.Text = $"Username: {_username}";

        // Start receiving messages in the background.
        _ = StartReceiving();
    }

    private async Task StartReceiving()
    {
        try
        {
            await _chatClient.StartReceivingAsync();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error receiving messages: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnMessageReceived(string message)
    {
        try
        {
            Dispatcher.Invoke(() =>
            {
                // Check if the message follows the "message:sender:content" structure.
                var parts = message.Split(':');
                if (parts.Length >= 3 && parts[0] == "message")
                {
                    var sender = parts[1];
                    var content = string.Join(":", parts.Skip(2));
                    ChatHistoryPanel.Children.Add(new TextBlock
                    {
                        Text = $"{sender}: {content}",
                        Margin = new Thickness(5)
                    });
                }
                else
                {
                    ChatHistoryPanel.Children.Add(new TextBlock
                    {
                        Text = message,
                        Margin = new Thickness(5)
                    });
                }
            });
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error updating UI: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void SendMessageButton_Click(object sender, RoutedEventArgs e)
    {
        if (_chatClient != null &&
            !string.IsNullOrWhiteSpace(MessageTextBox.Text) &&
            !string.IsNullOrWhiteSpace(ReceiverTextBox.Text))
        {
            var receiver = ReceiverTextBox.Text.Trim();
            var content = MessageTextBox.Text.Trim();
            try
            {
                // Use _username as the sender.
                await _chatClient.SendMessageAsync(_username, receiver, content);

                // Display the sent message in the chat history.
                Dispatcher.Invoke(() =>
                {
                    ChatHistoryPanel.Children.Add(new TextBlock
                    {
                        Text = $"You: {content}",
                        Margin = new Thickness(5),
                        HorizontalAlignment = HorizontalAlignment.Right
                    });
                });

                MessageTextBox.Clear();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Failed to send the message: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
