using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace LocalMessangerServer
{
    /// <summary>
    /// Interaction logic for UserSelectWindow.xaml
    /// </summary>
    public partial class UserSelectWindow : Window
    {
        public string SelectedUsername { get; private set; }

        public UserSelectWindow(string title, List<string> usernames)
        {
            InitializeComponent();

            this.Title = title;
            TitleTextBlock.Text = $"Select a user to {title.ToLower()}:";

            UsersListBox.ItemsSource = usernames;

            if (usernames.Count > 0)
            {
                UsersListBox.SelectedIndex = 0;
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersListBox.SelectedItem != null)
            {
                SelectedUsername = UsersListBox.SelectedItem.ToString();
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Please select a user.", "Selection Required");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}