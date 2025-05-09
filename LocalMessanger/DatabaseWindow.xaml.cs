using LocalMessangerServer.EF;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LocalMessangerServer
{
    /// <summary>
    /// Interaction logic for DatabaseWindow.xaml
    /// </summary>
    public partial class DatabaseWindow : Window
    {
        private readonly AppDbContext _db;
        private readonly LogService _logService;

        public DatabaseWindow()
        {
            InitializeComponent();
            _db = new AppDbContext();
            _logService = new LogService(_db);

            RefreshUsersData();
            RefreshMessagesData();
            RefreshBannedData();
            RefreshLogsData();
        }


        private void RefreshUsersData()
        {
            var users = _db.Users
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.CreatedAt,
                    u.Status
                })
                .ToList();

            UsersDataGrid.ItemsSource = users;
        }

        private void RefreshUsersButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshUsersData();
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedUser = UsersDataGrid.SelectedItem as dynamic;
            if (selectedUser == null)
            {
                MessageBox.Show("Please select a user to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to delete user '{selectedUser.Username}'? This will delete all messages and related data.",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var user = _db.Users.Find(selectedUser.Id);
                    if (user != null)
                    {
                        _db.Users.Remove(user);
                        _db.SaveChanges();
                        _logService.Log($"User '{selectedUser.Username}' was deleted", "Warning");
                        RefreshUsersData();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    _logService.Log($"Error deleting user: {ex.Message}", "Error");
                }
            }
        }
        private void RefreshMessagesData()
        {
            var messages = _db.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Select(m => new
                {
                    m.Id,
                    SenderName = m.Sender.Username,
                    ReceiverName = m.Receiver.Username,
                    m.Text,
                    m.SentAt
                })
                .OrderByDescending(m => m.SentAt)
                .Take(100)  // Limit to the last 100 messages for performance
                .ToList();

            MessagesDataGrid.ItemsSource = messages;
        }

        private void RefreshMessagesButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshMessagesData();
        }

        private void DeleteMessageButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedMessage = MessagesDataGrid.SelectedItem as dynamic;
            if (selectedMessage == null)
            {
                MessageBox.Show("Please select a message to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show("Are you sure you want to delete this message?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var message = _db.Messages.Find(selectedMessage.Id);
                    if (message != null)
                    {
                        _db.Messages.Remove(message);
                        _db.SaveChanges();
                        _logService.Log($"Message ID {selectedMessage.Id} was deleted", "Info");
                        RefreshMessagesData();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting message: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    _logService.Log($"Error deleting message: {ex.Message}", "Error");
                }
            }
        }

        private void RefreshBannedData()
        {
            var bannedUsers = _db.BannedUsers
                .Select(b => new
                {
                    b.Id,
                    b.Username,
                    b.BanDate,
                    ExpiryDate = b.BanDate.AddMonths(1)
                })
                .ToList();

            BannedUsersDataGrid.ItemsSource = bannedUsers;
        }

        private void RefreshBannedButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshBannedData();
        }

        private void UnbanUserButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedBan = BannedUsersDataGrid.SelectedItem as dynamic;
            if (selectedBan == null)
            {
                MessageBox.Show("Please select a user to unban.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var ban = _db.BannedUsers.Find(selectedBan.Id);
                if (ban != null)
                {
                    _db.BannedUsers.Remove(ban);
                    _db.SaveChanges();
                    _logService.Log($"User '{selectedBan.Username}' was unbanned", "Info");
                    RefreshBannedData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error unbanning user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _logService.Log($"Error unbanning user: {ex.Message}", "Error");
            }
        }
        private void RefreshLogsData()
        {
            var logs = _db.ServerLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(500)  // Limit to last 500 logs for performance
                .ToList();

            LogsDataGrid.ItemsSource = logs;
        }

        private void RefreshLogsButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshLogsData();
        }

        private void ClearLogsButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear all server logs?",
                "Confirm Clear", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Keep the most recent 10 logs for audit purposes
                    var logsToKeep = _db.ServerLogs
                        .OrderByDescending(l => l.Timestamp)
                        .Take(10)
                        .Select(l => l.Id)
                        .ToList();

                    var logsToDelete = _db.ServerLogs
                        .Where(l => !logsToKeep.Contains(l.Id))
                        .ToList();

                    _db.ServerLogs.RemoveRange(logsToDelete);
                    _db.SaveChanges();

                    _logService.Log("Server logs were cleared", "Warning");
                    RefreshLogsData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error clearing logs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    _logService.Log($"Error clearing logs: {ex.Message}", "Error");
                }
            }
        }

    }
}