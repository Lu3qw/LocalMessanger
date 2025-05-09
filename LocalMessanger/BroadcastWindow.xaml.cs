using System.Windows;

namespace LocalMessanger
{
    /// <summary>
    /// Interaction logic for BroadcastWindow.xaml
    /// </summary>
    public partial class BroadcastWindow : Window
    {
        public string MessageText => txtMessage.Text;

        public BroadcastWindow()
        {
            InitializeComponent();
            txtMessage.Focus();
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtMessage.Text))
                DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}