using System;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace MenubarDock.Services
{
    public class NetworkService : INotifyPropertyChanged
    {
        private readonly DispatcherTimer _timer;
        private bool _isConnected = true;
        private bool _isWifi = true;
        private string _statusText = "Connected";

        public bool IsConnected
        {
            get => _isConnected;
            private set { _isConnected = value; OnPropertyChanged(); }
        }

        public bool IsWifi
        {
            get => _isWifi;
            private set { _isWifi = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            private set { _statusText = value; OnPropertyChanged(); }
        }

        public NetworkService()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _timer.Tick += (s, e) => UpdateNetwork();
            UpdateNetwork();
            _timer.Start();
            NetworkChange.NetworkAvailabilityChanged += (s, e) => UpdateNetwork();
        }

        public void UpdateNetwork()
        {
            try
            {
                bool available = NetworkInterface.GetIsNetworkAvailable();
                IsConnected = available;
                IsWifi = true;

                if (!available)
                {
                    StatusText = "Disconnected";
                    return;
                }

                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    {
                        if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                            IsWifi = false;
                        else if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                            IsWifi = true;

                        StatusText = "Connected";
                        return;
                    }
                }
            }
            catch
            {
                IsConnected = true;
                StatusText = "Connected";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
