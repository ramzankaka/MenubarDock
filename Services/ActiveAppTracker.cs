using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Threading;
using static MenubarDock.Core.NativeMethods;

namespace MenubarDock.Services
{
    public class ActiveAppTracker : INotifyPropertyChanged
    {
        private readonly DispatcherTimer _timer;
        private string _activeAppName = "Finder";

        public string ActiveAppName
        {
            get => _activeAppName;
            private set { _activeAppName = value; OnPropertyChanged(); }
        }

        public ActiveAppTracker()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _timer.Tick += (s, e) => CheckForegroundApp();
            _timer.Start();
        }

        private void CheckForegroundApp()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return;

                var sbClass = new StringBuilder(256);
                GetClassName(hwnd, sbClass, sbClass.Capacity);
                string className = sbClass.ToString();

                if (className == "Progman" || className == "WorkerW")
                {
                    ActiveAppName = "Finder";
                    return;
                }

                GetWindowThreadProcessId(hwnd, out uint pid);
                if (pid == 0) return;

                var proc = Process.GetProcessById((int)pid);
                string name = proc.ProcessName;

                switch (name.ToLowerInvariant())
                {
                    case "shellexperiencehost":
                    case "applicationframehost":
                    case "searchhost":
                    case "startmenuexperiencehost":
                    case "lockapp":
                    case "taskbardock":
                    case "menubardock":
                        return; // Keep previous name
                    case "explorer":
                        name = "Finder";
                        break;
                    case "msedge":
                        name = "Microsoft Edge";
                        break;
                    case "chrome":
                        name = "Google Chrome";
                        break;
                    case "code":
                        name = "Visual Studio Code";
                        break;
                    case "notepad":
                        name = "Notepad";
                        break;
                    case "windowsterminal":
                    case "cmd":
                    case "powershell":
                        name = "Terminal";
                        break;
                    case "winword":
                        name = "Microsoft Word";
                        break;
                    case "excel":
                        name = "Microsoft Excel";
                        break;
                    case "firefox":
                        name = "Firefox";
                        break;
                }

                if (!string.IsNullOrEmpty(name))
                    ActiveAppName = char.ToUpperInvariant(name[0]) + name.Substring(1);
            }
            catch { }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
