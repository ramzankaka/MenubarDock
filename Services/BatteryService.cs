using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using MenubarDock.Core;
using static MenubarDock.Core.NativeMethods;

namespace MenubarDock.Services
{
    public class BatteryService : INotifyPropertyChanged
    {
        private readonly DispatcherTimer _timer;
        private int _percent = 100;
        private bool _isCharging;
        private bool _hasBattery = true;

        public int Percent
        {
            get => _percent;
            private set { _percent = value; OnPropertyChanged(); }
        }

        public bool IsCharging
        {
            get => _isCharging;
            private set { _isCharging = value; OnPropertyChanged(); }
        }

        public bool HasBattery
        {
            get => _hasBattery;
            private set { _hasBattery = value; OnPropertyChanged(); }
        }

        public BatteryService()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _timer.Tick += (s, e) => UpdateBattery();
            UpdateBattery();
            _timer.Start();
        }

        public void UpdateBattery()
        {
            if (GetSystemPowerStatus(out SYSTEM_POWER_STATUS status))
            {
                // BatteryFlag 128 means no system battery (Desktop PC)
                if (status.BatteryFlag == 128 || status.BatteryLifePercent == 255)
                {
                    HasBattery = false;
                    Percent = 100;
                    IsCharging = true;
                }
                else
                {
                    HasBattery = true;
                    Percent = Math.Clamp((int)status.BatteryLifePercent, 0, 100);
                    IsCharging = (status.ACLineStatus == 1) || ((status.BatteryFlag & 8) != 0);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
