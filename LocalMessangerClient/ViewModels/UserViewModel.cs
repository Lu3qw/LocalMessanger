using System.ComponentModel;
using System.Runtime.CompilerServices;
using LocalMessangerClient;

namespace LocalMessangerClient.ViewModels
{
    public class UserViewModel : INotifyPropertyChanged
    {
        public string Username { get; }

        private UserStatus _status;
        public UserStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();      
                }
            }
        }

        public UserViewModel(string username, UserStatus initialStatus = UserStatus.Offline)
        {
            Username = username;
            _status = initialStatus;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
