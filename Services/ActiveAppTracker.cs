using System;
using System.Collections.Concurrent;
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
        private bool _isDesktop = true;
        private bool _isFileExplorer = false;
        private IntPtr _lastActiveHwnd = IntPtr.Zero;

        private static readonly ConcurrentDictionary<string, string> _friendlyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["explorer"] = "Finder",
            ["code"] = "Visual Studio Code",
            ["chrome"] = "Google Chrome",
            ["msedge"] = "Microsoft Edge",
            ["firefox"] = "Firefox",
            ["brave"] = "Brave Browser",
            ["devenv"] = "Visual Studio",
            ["windowsterminal"] = "Terminal",
            ["cmd"] = "Terminal",
            ["powershell"] = "PowerShell",
            ["pwsh"] = "PowerShell",
            ["notepad"] = "Notepad",
            ["notepad++"] = "Notepad++",
            ["sublime_text"] = "Sublime Text",
            ["spotify"] = "Spotify",
            ["discord"] = "Discord",
            ["slack"] = "Slack",
            ["telegram"] = "Telegram",
            ["whatsapp"] = "WhatsApp",
            ["steam"] = "Steam",
            ["taskmgr"] = "Task Manager",
            ["winword"] = "Microsoft Word",
            ["excel"] = "Microsoft Excel",
            ["powerpnt"] = "Microsoft PowerPoint",
            ["outlook"] = "Microsoft Outlook",
            ["onenote"] = "Microsoft OneNote",
            ["rider64"] = "JetBrains Rider",
            ["idea64"] = "IntelliJ IDEA",
            ["pycharm64"] = "PyCharm",
            ["clion64"] = "CLion",
            ["webstorm64"] = "WebStorm",
            ["vlc"] = "VLC media player",
            ["obs64"] = "OBS Studio"
        };

        public string ActiveAppName
        {
            get => _activeAppName;
            private set { _activeAppName = value; OnPropertyChanged(); }
        }

        public bool IsDesktop
        {
            get => _isDesktop;
            private set { _isDesktop = value; OnPropertyChanged(); }
        }

        public bool IsFileExplorer
        {
            get => _isFileExplorer;
            private set { _isFileExplorer = value; OnPropertyChanged(); }
        }

        public IntPtr LastActiveHwnd
        {
            get => _lastActiveHwnd;
            private set => _lastActiveHwnd = value;
        }

        public ActiveAppTracker()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _timer.Tick += (s, e) => CheckForegroundApp();
            _timer.Start();
        }

        private void CheckForegroundApp()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero)
                {
                    IsDesktop = true;
                    IsFileExplorer = false;
                    return;
                }

                var sbClass = new StringBuilder(256);
                GetClassName(hwnd, sbClass, sbClass.Capacity);
                string className = sbClass.ToString();

                if (className.Equals("Progman", StringComparison.OrdinalIgnoreCase) ||
                    className.Equals("WorkerW", StringComparison.OrdinalIgnoreCase))
                {
                    ActiveAppName = "Finder";
                    IsDesktop = true;
                    IsFileExplorer = false;
                    return;
                }

                if (className.Equals("CabinetWClass", StringComparison.OrdinalIgnoreCase) ||
                    className.Equals("ExploreWClass", StringComparison.OrdinalIgnoreCase))
                {
                    ActiveAppName = "Finder";
                    IsDesktop = false;
                    IsFileExplorer = true;
                    LastActiveHwnd = hwnd;
                    return;
                }

                GetWindowThreadProcessId(hwnd, out uint pid);
                if (pid == 0) return;

                var proc = Process.GetProcessById((int)pid);
                string name = proc.ProcessName;

                // Handle UWP / Modern Windows apps hosted under ApplicationFrameHost
                if (name.Equals("applicationframehost", StringComparison.OrdinalIgnoreCase))
                {
                    var sbTitle = new StringBuilder(256);
                    GetWindowText(hwnd, sbTitle, sbTitle.Capacity);
                    string title = sbTitle.ToString().Trim();
                    if (!string.IsNullOrEmpty(title))
                    {
                        ActiveAppName = title;
                        IsDesktop = false;
                        IsFileExplorer = false;
                        LastActiveHwnd = hwnd;
                        return;
                    }
                }

                switch (name.ToLowerInvariant())
                {
                    case "shellexperiencehost":
                    case "searchhost":
                    case "startmenuexperiencehost":
                    case "lockapp":
                    case "taskbardock":
                    case "menubardock":
                    case "widgets":
                    case "textinputhost":
                        return; // Keep previous state for overlays and docks
                }

                LastActiveHwnd = hwnd;

                if (_friendlyNames.TryGetValue(name, out string? friendlyName))
                {
                    ActiveAppName = friendlyName;
                    IsDesktop = false;
                    IsFileExplorer = false;
                    return;
                }

                // Try reading FileDescription from process MainModule
                try
                {
                    string? desc = proc.MainModule?.FileVersionInfo?.FileDescription?.Trim();
                    if (!string.IsNullOrEmpty(desc) && desc.Length <= 35)
                    {
                        _friendlyNames[name] = desc;
                        ActiveAppName = desc;
                        IsDesktop = false;
                        IsFileExplorer = false;
                        return;
                    }
                }
                catch { }

                // Fallback to capitalized process name
                if (!string.IsNullOrEmpty(name))
                {
                    string formatted = char.ToUpperInvariant(name[0]) + name.Substring(1);
                    _friendlyNames[name] = formatted;
                    ActiveAppName = formatted;
                    IsDesktop = false;
                    IsFileExplorer = false;
                }
            }
            catch { }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
