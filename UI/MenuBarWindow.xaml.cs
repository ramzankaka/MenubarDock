using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;
using MenubarDock.Core;
using MenubarDock.Services;
using static MenubarDock.Core.NativeMethods;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using Brush = System.Windows.Media.Brush;
using Point = System.Windows.Point;

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
        private int _leaveTicks = 0;
        private double _dpiScale = 1.0;
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
            _networkService.PropertyChanged += (s, e) => Dispatcher.Invoke(UpdateNetworkUI);
            _appTracker.PropertyChanged += (s, e) => Dispatcher.Invoke(() =>
            {
                TxtActiveApp.Text = _appTracker.ActiveAppName;
                UpdateActiveAppMenu(_appTracker.ActiveAppName);
            });

            DpiChanged += (s, ev) =>
            {
                _dpiScale = ev.NewDpi.DpiScaleY;
                RepositionMenuBar();
            };

            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

            ApplyThemeStyles();
            UpdateBatteryUI();
            UpdateNetworkUI();
            UpdateActiveAppMenu(_appTracker.ActiveAppName);
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(RepositionMenuBar);
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            _hwnd = helper.Handle;

            int exStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
            SetWindowLong(_hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);

            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                _dpiScale = source.CompositionTarget.TransformToDevice.M22;
            }

            RepositionMenuBar();
        }

        public void ApplyThemeStyles()
        {
            string theme = _config.Settings.Theme ?? "DarkGlass";
            double opacity = _config.Settings.Opacity;

            Brush bgBrush;
            Color fg;
            Color iconColor;
            Brush borderBrush;

            switch (theme)
            {
                case "FrostedGlass":
                    bgBrush = new SolidColorBrush(Color.FromArgb((byte)(opacity * 255), 235, 238, 245));
                    fg = Color.FromRgb(20, 20, 28);
                    iconColor = fg;
                    borderBrush = new SolidColorBrush(Color.FromArgb(50, 0, 0, 0));
                    break;
                case "LightGlass":
                    bgBrush = new SolidColorBrush(Color.FromArgb((byte)(opacity * 255), 248, 248, 252));
                    fg = Color.FromRgb(30, 30, 38);
                    iconColor = fg;
                    borderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));
                    break;
                case "OLEDBlack":
                    bgBrush = new SolidColorBrush(Color.FromArgb((byte)(opacity * 255), 8, 8, 10));
                    fg = Color.FromRgb(255, 255, 255);
                    iconColor = fg;
                    borderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
                    break;
                case "DarkGlass":
                default:
                    var grad = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
                    grad.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(opacity * 255), 34, 34, 38), 0.0));
                    grad.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(opacity * 255), 22, 22, 24), 1.0));
                    bgBrush = grad;

                    var borderGrad = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
                    borderGrad.GradientStops.Add(new GradientStop(Color.FromArgb(80, 255, 255, 255), 0.0));
                    borderGrad.GradientStops.Add(new GradientStop(Color.FromArgb(35, 255, 255, 255), 1.0));
                    borderBrush = borderGrad;

                    fg = Color.FromRgb(240, 240, 245);
                    iconColor = Color.FromRgb(255, 255, 255);
                    break;
            }

            MenuBarContainer.Background = bgBrush;
            MenuBarContainer.BorderBrush = borderBrush;

            var fgBrush = new SolidColorBrush(fg);
            TxtActiveApp.Foreground = fgBrush;
            TxtClock.Foreground = fgBrush;
            TxtBatteryPercent.Foreground = fgBrush;

            // Update Left Menu buttons foreground
            BtnApple.Foreground = fgBrush;
            BtnActiveApp.Foreground = fgBrush;
            BtnFile.Foreground = fgBrush;
            BtnEdit.Foreground = fgBrush;
            BtnView.Foreground = fgBrush;
            BtnWindow.Foreground = fgBrush;
            BtnHelp.Foreground = fgBrush;

            // Icons
            var iconBrush = new SolidColorBrush(iconColor);
            PathApple.Fill = iconBrush;
            PathWifi.Fill = iconBrush;
            PathBluetooth.Fill = iconBrush;
            PathVolume.Fill = iconBrush;
            PathFocus.Fill = iconBrush;
            PathSearch.Fill = iconBrush;
            PathControlCenter.Fill = iconBrush;

            BorderBatteryOutline.BorderBrush = iconBrush;
            RectBatteryNub.Fill = iconBrush;
            RectBatteryLevel.Fill = new SolidColorBrush(Color.FromRgb(50, 215, 75));

            // Left side visibility
            BtnApple.Visibility = _config.Settings.ShowAppleMenu ? Visibility.Visible : Visibility.Collapsed;
            BtnActiveApp.Visibility = _config.Settings.ShowActiveApp ? Visibility.Visible : Visibility.Collapsed;

            // Widget visibility from settings
            BtnFocus.Visibility = _config.Settings.ShowFocusWidget ? Visibility.Visible : Visibility.Collapsed;
            BtnBluetooth.Visibility = _config.Settings.ShowBluetoothWidget ? Visibility.Visible : Visibility.Collapsed;
            BtnWifi.Visibility = _config.Settings.ShowWifiWidget ? Visibility.Visible : Visibility.Collapsed;
            BtnVolume.Visibility = _config.Settings.ShowVolumeWidget ? Visibility.Visible : Visibility.Collapsed;
            BtnSearch.Visibility = _config.Settings.ShowSearchWidget ? Visibility.Visible : Visibility.Collapsed;
            BtnControlCenter.Visibility = _config.Settings.ShowControlCenterWidget ? Visibility.Visible : Visibility.Collapsed;
            BtnClock.Visibility = _config.Settings.ShowClockWidget ? Visibility.Visible : Visibility.Collapsed;

            UpdateNetworkUI();
            UpdateBatteryUI();
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
            if (!IsVisible) return;

            // If auto-hide is disabled in settings, ensure bar is always fully visible
            if (!_config.Settings.AutoHideWhenAppsOpen)
            {
                if (_isCurrentlySlidUp)
                {
                    _isCurrentlySlidUp = false;
                    _leaveTicks = 0;
                    SlideBar(0);
                }
                return;
            }

            // If a dropdown context menu is currently open, keep bar visible
            if (_isMenuOpen)
            {
                _leaveTicks = 0;
                if (_isCurrentlySlidUp)
                {
                    _isCurrentlySlidUp = false;
                    SlideBar(0);
                }
                return;
            }

            if (!GetCursorPos(out POINT pt)) return;

            // Check if any application or program window is open and restored on screen
            bool hasOpenWindows = WindowDetectionService.HasOpenApplicationWindows();
            bool isDesktopActive = _appTracker.IsDesktop;

            // When no programs are open (clean desktop or all windows minimized) or desktop is focused,
            // the menu bar stays fully visible.
            if (!hasOpenWindows || isDesktopActive)
            {
                _leaveTicks = 0;
                if (_isCurrentlySlidUp)
                {
                    _isCurrentlySlidUp = false;
                    SlideBar(0);
                }
                return;
            }

            // An application window is open and active. Auto-hide applies unless pointer touches top edge.
            double physicalBarHeight = Height * _dpiScale;
            double physicalScreenWidth = SystemParameters.PrimaryScreenWidth * _dpiScale;

            bool withinHorizontalBounds = pt.X >= 0 && pt.X <= physicalScreenWidth;
            bool atTopEdge = withinHorizontalBounds && pt.Y >= 0 && pt.Y <= 2;
            bool overBar = !_isCurrentlySlidUp && withinHorizontalBounds && pt.Y >= 0 && pt.Y <= (physicalBarHeight + 6 * _dpiScale);

            if (atTopEdge)
            {
                _leaveTicks = 0;
                if (_isCurrentlySlidUp)
                {
                    _isCurrentlySlidUp = false;
                    SlideBar(0);
                }
            }
            else if (overBar)
            {
                _leaveTicks = 0;
                // Pointer is hovering or interacting with the menu bar: stay visible
            }
            else
            {
                // Pointer is outside the menu bar: debounce before sliding up
                if (!_isCurrentlySlidUp)
                {
                    _leaveTicks++;
                    if (_leaveTicks >= 6) // ~210ms debounce to prevent accidental dismissals
                    {
                        _isCurrentlySlidUp = true;
                        SlideBar(-(Height + 4));
                    }
                }
                else
                {
                    _leaveTicks = 0;
                }
            }
        }

        private void SlideBar(double targetY)
        {
            Dispatcher.Invoke(() =>
            {
                var anim = new DoubleAnimation(targetY, TimeSpan.FromMilliseconds(200))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
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

            BtnBattery.ToolTip = _batteryService.IsCharging
                ? $"Battery: {_batteryService.Percent}% (Charging)"
                : $"Battery: {_batteryService.Percent}%";
        }

        private void UpdateNetworkUI()
        {
            if (!_config.Settings.ShowWifiWidget)
            {
                BtnWifi.Visibility = Visibility.Collapsed;
                return;
            }

            BtnWifi.Visibility = Visibility.Visible;

            if (!_networkService.IsConnected)
            {
                PathWifi.Opacity = 0.35;
                BtnWifi.ToolTip = "Network: Disconnected";
            }
            else
            {
                PathWifi.Opacity = 1.0;
                BtnWifi.ToolTip = _networkService.IsWifi
                    ? $"Wi-Fi: {_networkService.StatusText}"
                    : $"Ethernet: {_networkService.StatusText}";
            }
        }

        private void UpdateActiveAppMenu(string appName)
        {
            MenuAboutApp.Header = $"About {appName}";
            MenuHideApp.Header = $"Hide {appName}  (Win+Down)";
            MenuQuitApp.Header = $"Quit {appName}  (Alt+F4)";
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

        // Active App Menu Handlers
        private void OnAboutActiveAppClick(object sender, RoutedEventArgs e)
        {
            string app = _appTracker.ActiveAppName;
            if (app.Equals("Finder", StringComparison.OrdinalIgnoreCase))
            {
                SystemActions.OpenAboutThisPC();
            }
            else
            {
                System.Windows.MessageBox.Show($"{app}\n\nRunning on Windows 11 with MenubarDock.", $"About {app}", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void OnHideActiveAppClick(object sender, RoutedEventArgs e) => SystemActions.MinimizeWindow(_appTracker.LastActiveHwnd);
        private void OnHideOthersClick(object sender, RoutedEventArgs e) => SystemActions.HideOthers(_appTracker.LastActiveHwnd);
        private void OnShowAllClick(object sender, RoutedEventArgs e) => SystemActions.ShowAll();
        private void OnQuitActiveAppClick(object sender, RoutedEventArgs e) => SystemActions.CloseActiveWindow(_appTracker.LastActiveHwnd);

        // Apple System Menu
        private void OnAboutThisPcClick(object sender, RoutedEventArgs e) => SystemActions.OpenAboutThisPC();
        private void OnSystemSettingsClick(object sender, RoutedEventArgs e) => SystemActions.OpenSystemSettings();
        private void OnTaskManagerClick(object sender, RoutedEventArgs e) => SystemActions.OpenTaskManager();
        private void OnMenuBarSettingsClick(object sender, RoutedEventArgs e) => _openSettingsAction();
        private void OnLockScreenClick(object sender, RoutedEventArgs e) => SystemActions.LockComputer();
        private void OnSleepClick(object sender, RoutedEventArgs e) => SystemActions.SleepComputer();
        private void OnRestartClick(object sender, RoutedEventArgs e) => SystemActions.RestartComputerSafe();
        private void OnShutdownClick(object sender, RoutedEventArgs e) => SystemActions.ShutdownComputerSafe();

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
        private void OnCloseActiveWindowClick(object sender, RoutedEventArgs e) => SystemActions.CloseActiveWindow(_appTracker.LastActiveHwnd);

        // Edit Menu
        private void OnUndoClick(object sender, RoutedEventArgs e) => SystemActions.TriggerUndo(_appTracker.LastActiveHwnd);
        private void OnRedoClick(object sender, RoutedEventArgs e) => SystemActions.TriggerRedo(_appTracker.LastActiveHwnd);
        private void OnCutClick(object sender, RoutedEventArgs e) => SystemActions.TriggerCut(_appTracker.LastActiveHwnd);
        private void OnCopyClick(object sender, RoutedEventArgs e) => SystemActions.TriggerCopy(_appTracker.LastActiveHwnd);
        private void OnPasteClick(object sender, RoutedEventArgs e) => SystemActions.TriggerPaste(_appTracker.LastActiveHwnd);
        private void OnSelectAllClick(object sender, RoutedEventArgs e) => SystemActions.TriggerSelectAll(_appTracker.LastActiveHwnd);
        private void OnClipboardClick(object sender, RoutedEventArgs e) => SystemActions.OpenClipboard();

        // View Menu
        private void OnFullscreenClick(object sender, RoutedEventArgs e) => SystemActions.TriggerFullscreen(_appTracker.LastActiveHwnd);
        private void OnZoomInClick(object sender, RoutedEventArgs e) => SystemActions.TriggerZoomIn(_appTracker.LastActiveHwnd);
        private void OnZoomOutClick(object sender, RoutedEventArgs e) => SystemActions.TriggerZoomOut(_appTracker.LastActiveHwnd);
        private void OnZoomResetClick(object sender, RoutedEventArgs e) => SystemActions.TriggerZoomReset(_appTracker.LastActiveHwnd);
        private void OnTaskViewClick(object sender, RoutedEventArgs e) => SystemActions.OpenTaskView();
        private void OnShowDesktopClick(object sender, RoutedEventArgs e) => SystemActions.ShowDesktop();

        // Window Menu
        private void OnMinimizeClick(object sender, RoutedEventArgs e) => SystemActions.MinimizeWindow(_appTracker.LastActiveHwnd);
        private void OnMaximizeClick(object sender, RoutedEventArgs e) => SystemActions.MaximizeWindow(_appTracker.LastActiveHwnd);
        private void OnSnapLeftClick(object sender, RoutedEventArgs e) => SystemActions.SnapWindowLeft(_appTracker.LastActiveHwnd);
        private void OnSnapRightClick(object sender, RoutedEventArgs e) => SystemActions.SnapWindowRight(_appTracker.LastActiveHwnd);

        // Volume Handlers
        private void OnVolumeMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0) SystemActions.VolumeUp();
            else if (e.Delta < 0) SystemActions.VolumeDown();
            e.Handled = true;
        }
        private void OnVolumeMuteClick(object sender, RoutedEventArgs e) => SystemActions.VolumeMute();
        private void OnVolumeUpClick(object sender, RoutedEventArgs e) => SystemActions.VolumeUp();
        private void OnVolumeDownClick(object sender, RoutedEventArgs e) => SystemActions.VolumeDown();

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

        protected override void OnClosed(EventArgs e)
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            base.OnClosed(e);
        }
    }
}