using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace LocalMessangerClient
{

    public enum UserStatus { Online, Offline, DoNotDisturb, Away }

    public partial class MainWindow : Window
    {
        private ChatClient? _chatClient;
        private readonly string _username;
        public UserStatus SelectedUserStatus { get; set; }
        public ObservableCollection<string> AllUsers { get; } = new ObservableCollection<string>();
        private CollectionViewSource FilteredUsersView { get; } = new CollectionViewSource();
        public ObservableCollection<MessageViewModel> Messages { get; } = new();
        public MainWindow(ChatClient chatClient, string username)
        {
            InitializeComponent();
            _chatClient = chatClient;
            _username = username;

            UsernameDisplayTextBlock.Text = $"Username: {_username}";
            _chatClient.MessageReceived += OnMessageReceived;

            FilteredUsersView.Source = AllUsers;
            FilteredUsersView.Filter += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(UserSearchTextBox.Text))
                    e.Accepted = true;
                else
                    e.Accepted = ((string)e.Item).IndexOf(UserSearchTextBox.Text, StringComparison.OrdinalIgnoreCase) >= 0;
            };
            ChatListBox.ItemsSource = FilteredUsersView.View;
            ChatHistoryList.ItemsSource = Messages;
            SelectedUserStatus = UserStatus.Online;


            _ = LoadUserListAsync();
        }

        public class MessageViewModel
        {
            public string Content { get; set; }
            public string Time { get; set; }
            public bool IsMe { get; set; }
        }

        private async Task LoadUserListAsync()
        {
            var response = await _chatClient!.SendRequestAsync("list_users");
            var users = response.Split(',', StringSplitOptions.RemoveEmptyEntries);
            Application.Current.Dispatcher.Invoke(() =>
            {
                AllUsers.Clear();
                foreach (var u in users.Where(u => u != _username))
                    AllUsers.Add(u);
            });
        }

        private void OnMessageReceived(string message)
        {
            Dispatcher.Invoke(() =>
            {
                if (message.StartsWith("notify_status:"))
                {
                    var parts = message.Split(':');
                    var who = parts[1];
                    var st = (UserStatus)Enum.Parse(typeof(UserStatus), parts[2]);

                    UpdateUserStatusInUI(who, st);
                    return;
                }
                if (!message.StartsWith("message:")) return;
                var body = message.Substring("message:".Length);
                var pipe = body.LastIndexOf('|');
                if (pipe < 0) return;

                var head = body.Substring(0, pipe);      // "sender:content"
                var tsText = body.Substring(pipe + 1);   // timestamp
                var colon = head.IndexOf(':');
                if (colon < 0) return;

                var sender = head.Substring(0, colon);
                var content = head.Substring(colon + 1);

                if (!DateTime.TryParse(tsText, null,
                      System.Globalization.DateTimeStyles.RoundtripKind, out var sentAt))
                    sentAt = DateTime.UtcNow;

                Messages.Add(new MessageViewModel
                {
                    Content = content,
                    Time = sentAt.ToLocalTime().ToString("HH:mm"),
                    IsMe = sender == _username
                });
            });
        }

        private async void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChatListBox.SelectedItem is not string selectedUser)
            {
                MessageBox.Show("Select chat.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var content = MessageTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(content)) return;

            await _chatClient!.SendMessageAsync(_username, selectedUser, content);

            Messages.Add(new MessageViewModel
            {
                Content = content,
                Time = DateTime.Now.ToString("HH:mm"),
                IsMe = true
            });
            MessageTextBox.Clear();
        }


        private void UserSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilteredUsersView.View.Refresh();
        }

        private async void ChatListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChatListBox.SelectedItem is string partner)
                await LoadChatHistoryAsync(partner);
        }

        private async Task LoadChatHistoryAsync(string partner)
        {
            Messages.Clear();

            var response = await _chatClient!.SendRequestAsync($"get_history:{_username}:{partner}");
            var entries = response.Split('|', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < entries.Length; i += 2)
            {
                var head = entries[i];       // "sender:content"
                var tsText = entries[i + 1]; // timestamp

                var colon = head.IndexOf(':');
                if (colon < 0) continue;

                var sender = head.Substring(0, colon);
                var content = head.Substring(colon + 1);

                if (!DateTime.TryParse(tsText, null,
                      System.Globalization.DateTimeStyles.RoundtripKind, out var sentAt))
                    sentAt = DateTime.UtcNow;

                Messages.Add(new MessageViewModel
                {
                    Content = content,
                    Time = sentAt.ToLocalTime().ToString("HH:mm"),
                    IsMe = sender == _username
                });
            }
        }


        private void UpdateUserStatusInUI(string user, UserStatus status)
        {
            // наприклад, змінюємо колір фону в списку
            var item = ChatListBox.Items.Cast<string>().FirstOrDefault(u => u == user);
            if (item != null)
            {
                var container = (ListBoxItem)ChatListBox.ItemContainerGenerator.ContainerFromItem(item);
                if (container != null)
                {
                    container.Background = status switch
                    {
                        UserStatus.Online => Brushes.LightGreen,
                        UserStatus.Offline => Brushes.LightGray,
                        UserStatus.DoNotDisturb => Brushes.IndianRed,
                        UserStatus.Away => Brushes.Khaki,
                        _ => Brushes.White
                    };
                }
            }
        }


        private void EmojiToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            EmojiPopup.IsOpen = true;
        }

        private void EmojiToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            EmojiPopup.IsOpen = false;
        }

        private void EmojiPopup_Closed(object sender, EventArgs e)
        {
            EmojiToggleButton.IsChecked = false;
        }

        private void EmojiButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content is string emoji)
            {
                var tb = MessageTextBox;
                int idx = tb.CaretIndex;
                tb.Text = tb.Text.Insert(idx, emoji);
                tb.CaretIndex = idx + emoji.Length;
                tb.Focus();
            }
        }

        private async void BlockButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChatListBox.SelectedItem is not string toBlock) return;
            var result = await _chatClient.BlockUserAsync(_username, toBlock);
            if (result == "success")
                MessageBox.Show($"You blocked {toBlock}");
            else
                MessageBox.Show($"Block failed: {result}");
        }

        private async void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_chatClient == null) return;
            await _chatClient.ChangeStatusAsync(_username, SelectedUserStatus.ToString());
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _chatClient?.Disconnect();
        }
    }
}
