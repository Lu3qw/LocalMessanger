using System.ComponentModel;
using System.Runtime.CompilerServices;
using LocalMessangerServer.EF;
using LocalMessanger;

namespace LocalMessangerServer.ViewModels
{
    public class ClientInfoViewModel : INotifyPropertyChanged
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

        public ClientInfoViewModel(string username, UserStatus initialStatus = UserStatus.Offline)
        {
            Username = username;
            _status = initialStatus;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
