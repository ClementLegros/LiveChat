namespace LiveChat.Client
{
    using System.ComponentModel;

    public class User : INotifyPropertyChanged
    {
        public string Username { get; set; }
        public string IpAddress { get; set; }
        
        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged(nameof(IsConnected));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return $"{Username} - {IpAddress}";
        }
    }
}