using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MenubarDock.Core;
using MenubarDock.Services;
using static MenubarDock.Core.NativeMethods;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;

namespace MenubarDock.UI
{
    public partial class MenuBarWindow : Window
    {
        private readonly ConfigurationManager _config;
        private readonly BatteryService _batteryService;
        private readonly NetworkService _networkService;
        private readonly ActiveAppTracker _appTracker;
        private readonly DispatcherTimer _clockTimer;
        private readonly DispatcherTimer _autoHideTimer;
        private readonly Action _openSettingsAction;

        private bool _isCurrentlySlidUp;
        private bool _isMenuOpen;
        private IntPtr _hwnd = IntPtr.Zero;

        public MenuBarWindow(ConfigurationManager config, BatteryService batteryService,
            NetworkService networkService, ActiveAppTracker appTracker, Action openSettingsAction)
        {
            _config = config;
            _batteryService = batteryService;
            _networkService = networkService;
            _appTracker = appTracker;
            _openSettingsAction = openSettingsAction;

            InitializeComponent();

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => UpdateClock();
            _clockTimer.Start();
            UpdateClock();

            _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(35) };
            _autoHideTimer.Tick += (s, e) => CheckTopEdgeProximity();
            _autoHideTimer.Start();

            _batteryService.PropertyChanged += (s, e) => Dispatcher.Invoke(UpdateBatteryUI);
            _appTracker.PropertyChanged += (s, e) =>
                Dispatcher.Invoke(() => TxtActiveApp.Text = _appTracker.ActiveAppName);

            ApplyThemeStyles();
            UpdateBatteryUI();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            _hwnd = helper.Handle;

            int exStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
            SetWindowLong(_hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);

            RepositionMenuBar();
        }

        public void ApplyThemeStyles()
        {
            string theme = _config.Settings.Theme ?? "FrostedGlass";
            double opacity = _config.Settings.Opacity;

            Color bg;
            Color fg;
            Color iconColor;

            switch (theme)
            {
                case "FrostedGlass":
                    bg = Color.FromArgb((byte)(opacity * 255), 225, 228, 238);
                    fg = Color.FromRgb(20, 20, 28);
                    iconColor = fg;
                    break;
                case "DarkGlass":
                    bg = Color.FromArgb((byte)(opacity * 255), 28, 28, 32);
                    fg = Color.FromRgb(240, 240, 245);
                    iconColor = fg;
                    break;
                case "LightGlass":
                    bg = Color.FromArgb((byte)(opacity * 255), 248, 248, 252);
                    fg = Color.FromRgb(30, 30, 38);
                    iconColor = fg;
                    break;
                case "OLEDBlack":
                    bg = Color.FromArgb((byte)(opacity * 255), 8, 8, 10);
                    fg = Color.FromRgb(255, 255, 255);
                    iconColor = fg;
                    break;
                default:
                    bg = Color.FromArgb((byte)(opacity * 255), 225, 228, 238);
                    fg = Color.FromRgb(20, 20, 28);
                    iconColor = fg;
                    break;
            }

            MenuBarContainer.Background = new SolidColorBrush(bg);

            var fgBrush = new SolidColorBrush(fg);
            TxtActiveApp.Foreground = fgBrush;
            TxtClock.Foreground = fgBrush;
            TxtBatteryPercent.Foreground = fgBrush;

            var iconBrush = new SolidColorBrush(iconColor);
            PathApple.Fill = iconBrush;
            PathWifi.Fill = iconBrush;
            PathBluetooth.Fill = iconBrush;
            PathVolume.Fill = iconBrush;
            PathFocus.Fill = iconBrush;
            PathSearch.Fill = iconBrush;
            PathControlCenter.Fill = iconBrush;

            // Update battery border color
            RectBatteryLevel.Fill = new SolidColorBrush(Color.FromRgb(50, 215, 75));

            // Widget visibility from settings
            BtnFocus.Visibility = _config.Settings.ShowFocusWidget ? Visibility.Visible : Visibility.Collapsed;
            BtnBluetooth.Visibility = _config.Settings.ShowBluetoothWidget ? Visibility.Visible : Visibility.Collapsed;
            BtnWifi.Visibility = _config.Settings.ShowWifiWidget ? Visibility.Visible : Visibility.Collapsed;
            BtnVolume.Visibility = _config.Settings.ShowVolumeWidget ? Visibility.Visible : Visibility.Collapsed;
            BtnSearch.Visibility = _config.Settings.ShowSearchWidget ? Visibility.Visible : Visibility.Collapsed;
            BtnControlCenter.Visibility = _config.Settings.ShowControlCenterWidget ? Visibility.Visible : Visibility.Collapsed;

            RepositionMenuBar();
        }

        public void RepositionMenuBar()
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            Left = 0;
            Top = 0;
            Width = screenWidth;
            Height = _config.Settings.BarHeight;
        }

        private void CheckTopEdgeProximity()
        {
            if (!_config.Settings.AutoHideWhenAppsOpen || !IsVisible || _isMenuOpen) return;
            if (!GetCursorPos(out POINT pt)) return;

            bool isDesktopActive = _appTracker.ActiveAppName.Equals("Finder",
                StringComparison.OrdinalIgnoreCase);

            if (isDesktopActive)
            {
                if (_isCurrentlySlidUp) { _isCurrentlySlidUp = false; SlideBar(0); }
                return;
            }

            bool atTopEdge = pt.Y <= 2;
            bool overBar = pt.Y <= (Height + 4);

            if (atTopEdge && _isCurrentlySlidUp)
            {
                _isCurrentlySlidUp = false;
                SlideBar(0);
            }
            else if (!overBar && !_isCurrentlySlidUp)
            {
                _isCurrentlySlidUp = true;
                SlideBar(-(Height + 2));
            }
        }

        private void SlideBar(double targetY)
        {
            Dispatcher.Invoke(() =>
            {
                var anim = new DoubleAnimation(targetY, TimeSpan.FromMilliseconds(180))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                MenuBarSlideTransform.BeginAnimation(TranslateTransform.YProperty, anim);
            });
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            string fmt = _config.Settings.Clock24Hour ? "HH:mm" : "h:mm tt";
            if (_config.Settings.ClockShowSeconds)
                fmt = _config.Settings.Clock24Hour ? "HH:mm:ss" : "h:mm:ss tt";
            string date = _config.Settings.ClockShowDate ? $"{now:ddd MMM d}  " : "";
            TxtClock.Text = $"{date}{now.ToString(fmt)}";
        }

        private void UpdateBatteryUI()
        {
            if (!_batteryService.HasBattery || !_config.Settings.ShowBatteryWidget)
            {
                BtnBattery.Visibility = Visibility.Collapsed;
                return;
            }

            BtnBattery.Visibility = Visibility.Visible;
            TxtBatteryPercent.Text = $"{_batteryService.Percent}%";

            double fill = Math.Max(2.0, (15.0 * _batteryService.Percent) / 100.0);
            RectBatteryLevel.Width = fill;

            RectBatteryLevel.Fill = new SolidColorBrush(
                _batteryService.Percent <= 20 && !_batteryService.IsCharging
                    ? Color.FromRgb(255, 69, 58)
                    : Color.FromRgb(50, 215, 75));

            PathChargingBolt.Visibility = _batteryService.IsCharging
                ? Visibility.Visible : Visibility.Collapsed;
        }

        // Generic dropdown opener for all menu buttons
        private void OnOpenMenuClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
                _isMenuOpen = true;
                btn.ContextMenu.Closed += (s, _) => _isMenuOpen = false;
            }
        }

        // Apple System Menu
        private void OnAboutThisPcClick(object sender, RoutedEventArgs e) => SystemActions.OpenAboutThisPC();
        private void OnSystemSettingsClick(object sender, RoutedEventArgs e) => SystemActions.OpenSystemSettings();
        private void OnTaskManagerClick(object sender, RoutedEventArgs e) => SystemActions.OpenTaskManager();
        private void OnMenuBarSettingsClick(object sender, RoutedEventArgs e) => _openSettingsAction();
        private void OnLockScreenClick(object sender, RoutedEventArgs e) => SystemActions.LockComputer();
        private void OnSleepClick(object sender, RoutedEventArgs e) => SystemActions.SleepComputer();
        private void OnRestartClick(object sender, RoutedEventArgs e) => SystemActions.RestartComputer();
        private void OnShutdownClick(object sender, RoutedEventArgs e) => SystemActions.ShutdownComputer();

        // File Menu
        private void OnNewExplorerClick(object sender, RoutedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo { FileName = "explorer.exe", UseShellExecute = true }); } catch { }
        }
        private void OnNewTerminalClick(object sender, RoutedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo { FileName = "wt.exe", UseShellExecute = true }); }
            catch { try { Process.Start(new ProcessStartInfo { FileName = "cmd.exe", UseShellExecute = true }); } catch { } }
        }
        private void OnNewNotepadClick(object sender, RoutedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo { FileName = "notepad.exe", UseShellExecute = true }); } catch { }
        }
        private void OnCloseActiveWindowClick(object sender, RoutedEventArgs e) => SystemActions.CloseActiveWindow();

        // Edit Menu
        private void OnUndoClick(object sender, RoutedEventArgs e) => SystemActions.TriggerUndo();
        private void OnRedoClick(object sender, RoutedEventArgs e) => SystemActions.TriggerRedo();
        private void OnCutClick(object sender, RoutedEventArgs e) => SystemActions.TriggerCut();
        private void OnCopyClick(object sender, RoutedEventArgs e) => SystemActions.TriggerCopy();
        private void OnPasteClick(object sender, RoutedEventArgs e) => SystemActions.TriggerPaste();
        private void OnSelectAllClick(object sender, RoutedEventArgs e) => SystemActions.TriggerSelectAll();
        private void OnClipboardClick(object sender, RoutedEventArgs e) => SystemActions.OpenClipboard();

        // View Menu
        private void OnFullscreenClick(object sender, RoutedEventArgs e) => SystemActions.TriggerFullscreen();
        private void OnZoomInClick(object sender, RoutedEventArgs e) => SystemActions.TriggerZoomIn();
        private void OnZoomOutClick(object sender, RoutedEventArgs e) => SystemActions.TriggerZoomOut();
        private void OnZoomResetClick(object sender, RoutedEventArgs e) => SystemActions.TriggerZoomReset();
        private void OnTaskViewClick(object sender, RoutedEventArgs e) => SystemActions.OpenTaskView();
        private void OnShowDesktopClick(object sender, RoutedEventArgs e) => SystemActions.ShowDesktop();

        // Window Menu
        private void OnMinimizeClick(object sender, RoutedEventArgs e) => SystemActions.MinimizeWindow();
        private void OnMaximizeClick(object sender, RoutedEventArgs e) => SystemActions.MaximizeWindow();
        private void OnSnapLeftClick(object sender, RoutedEventArgs e) => SystemActions.SnapWindowLeft();
        private void OnSnapRightClick(object sender, RoutedEventArgs e) => SystemActions.SnapWindowRight();

        // Widget-specific dropdowns (open ONLY their specific settings)
        private void OnBatterySettingsClick(object sender, RoutedEventArgs e) => SystemActions.OpenPowerBatterySettings();
        private void OnBatterySaverClick(object sender, RoutedEventArgs e) => SystemActions.OpenBatterySaverSettings();
        private void OnWifiSettingsClick(object sender, RoutedEventArgs e) => SystemActions.OpenWifiSettings();
        private void OnNetworkSettingsClick(object sender, RoutedEventArgs e) => SystemActions.OpenNetworkSettings();
        private void OnAirplaneModeClick(object sender, RoutedEventArgs e) => SystemActions.OpenAirplaneModeSettings();
        private void OnBluetoothSettingsClick(object sender, RoutedEventArgs e) => SystemActions.OpenBluetoothSettings();
        private void OnSoundSettingsClick(object sender, RoutedEventArgs e) => SystemActions.OpenSoundSettings();
        private void OnVolumeMixerClick(object sender, RoutedEventArgs e) => SystemActions.OpenVolumeMixer();
        private void OnFocusSettingsClick(object sender, RoutedEventArgs e) => SystemActions.OpenFocusSettings();
        private void OnNotificationSettingsClick(object sender, RoutedEventArgs e) => SystemActions.OpenNotificationSettings();

        // Search, Control Center, Clock
        private void OnSearchClick(object sender, RoutedEventArgs e) => SystemActions.OpenWindowsSearch();
        private void OnQuickSettingsClick(object sender, RoutedEventArgs e) => SystemActions.OpenQuickSettings();
        private void OnClockClick(object sender, RoutedEventArgs e) => SystemActions.OpenNotifications();
    }
}