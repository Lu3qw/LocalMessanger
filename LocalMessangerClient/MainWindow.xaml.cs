using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using LocalMessangerClient.ViewModels;

namespace LocalMessangerClient
{
    public enum UserStatus { Online, Offline, DoNotDisturb, Away }

    public partial class MainWindow : Window
    {
        private ChatClient? _chatClient;
        private readonly string _username;
        public UserStatus SelectedUserStatus { get; set; }
        public ObservableCollection<UserViewModel> AllUsers { get; } = new ObservableCollection<UserViewModel>();
        private CollectionViewSource FilteredUsersView { get; } = new CollectionViewSource();
        public ObservableCollection<MessageViewModel> Messages { get; } = new();

        private UserViewModel? _selectedUserViewModel;

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
                else if (e.Item is UserViewModel user)
                    e.Accepted = user.Username.IndexOf(UserSearchTextBox.Text, StringComparison.OrdinalIgnoreCase) >= 0;
                else
                    e.Accepted = false;
            };

            ChatListBox.ItemsSource = FilteredUsersView.View;
            ChatHistoryList.ItemsSource = Messages;
            SelectedUserStatus = UserStatus.Online;

            _ = LoadUserListAsync();
            DataContext = this;
        }

        private async Task LoadUserListAsync()
        {
            var response = await _chatClient!.SendRequestAsync("list_users");
            var users = response.Split(',', StringSplitOptions.RemoveEmptyEntries);

            Application.Current.Dispatcher.Invoke(() =>
            {
                AllUsers.Clear();
                foreach (var u in users.Where(u => u != _username))
                {
                    AllUsers.Add(new UserViewModel
                    {
                        Username = u,
                        Status = UserStatus.Offline
                    });
                }
            });

            var blocksResp = await _chatClient.SendRequestAsync($"get_blocks:{_username}");
            var blockedNames = blocksResp.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var uvm in AllUsers)
                uvm.IsBlocked = blockedNames.Contains(uvm.Username, StringComparer.OrdinalIgnoreCase);

            var statusResponse = await _chatClient.SendRequestAsync("get_statuses");
            var pairs = statusResponse.Split('|', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var parts = pair.Split(':');
                if (parts.Length == 2 && Enum.TryParse(parts[1], out UserStatus status))
                {
                    UpdateUserStatusInUI(parts[0], status);
                }
            }
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
            if (_selectedUserViewModel == null)
            {
                MessageBox.Show("Select chat.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var content = MessageTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(content)) return;

            await _chatClient!.SendMessageAsync(_username, _selectedUserViewModel.Username, content);

            Messages.Add(new MessageViewModel
            {
                Content = content,
                Time = DateTime.Now.ToString("HH:mm"),
                IsMe = true
            });
            MessageTextBox.Clear();
        }

        private async Task LoadChatHistoryAsync(string partner)
        {
            Messages.Clear();

            var response = await _chatClient!.SendRequestAsync($"get_history:{_username}:{partner}");
            var entries = response.Split('|', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < entries.Length; i += 2)
            {
                if (i + 1 >= entries.Length) break;

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

        private void UpdateUserStatusInUI(string username, UserStatus status)
        {
            var userVM = AllUsers.FirstOrDefault(u => u.Username == username);
            if (userVM != null)
            {
                userVM.Status = status;
            }
        }

        private void UserSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilteredUsersView.View.Refresh();
        }

        private async void ChatListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChatListBox.SelectedItem is UserViewModel selectedUser)
            {
                _selectedUserViewModel = selectedUser;
                await LoadChatHistoryAsync(selectedUser.Username);
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
            if (_selectedUserViewModel == null) return;

            var target = _selectedUserViewModel.Username;
            var result = await _chatClient.SendRequestAsync($"block:{_username}:{target}");

            if (result == "blocked")
            {
                _selectedUserViewModel.IsBlocked = true;
                MessageBox.Show($"You blocked {target}");
            }
            else if (result == "unblocked")
            {
                _selectedUserViewModel.IsBlocked = false;
                MessageBox.Show($"You unblocked {target}");
            }
            else
            {
                MessageBox.Show($"Error: {result}");
            }
        }

        private async void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_chatClient == null) return;
            if (sender is ComboBox cb && cb.SelectedItem is UserStatus st)
            {
                await _chatClient.ChangeStatusAsync(_username, st.ToString());
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _chatClient.ChangeStatusAsync(_username, UserStatus.Offline.ToString());
            _chatClient?.Disconnect();
        }

        private async void ChatListUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadUserListAsync();
        }
    }
}