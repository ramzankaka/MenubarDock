using System;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using MenubarDock.Core;
using MenubarDock.Diagnostics;
using MenubarDock.Services;
using MenubarDock.UI;

namespace MenubarDock
{
    public partial class App : System.Windows.Application
    {
        private const string MutexName = "MenubarDock_SingleInstance_Mutex_90D5";
        private Mutex? _instanceMutex;

        private ConfigurationManager? _config;
        private BatteryService? _batteryService;
        private NetworkService? _networkService;
        private ActiveAppTracker? _appTracker;
        private MenuBarWindow? _menuBarWindow;
        private SettingsWindow? _settingsWindow;
        private TrayController? _trayController;
        private GlobalHotkeyManager? _hotkeyManager;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _instanceMutex = new Mutex(true, MutexName, out bool isNewInstance);
            if (!isNewInstance)
            {
                System.Windows.MessageBox.Show("MenubarDock is already running in the system tray.", "MenubarDock", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            try
            {
                InitializeApp();
            }
            catch (Exception ex)
            {
                Logger.Error("Fatal exception during startup", ex);
                Shutdown();
            }
        }

        private void InitializeApp()
        {
            _config = new ConfigurationManager();
            _config.LoadSettings();

            _batteryService = new BatteryService();
            _networkService = new NetworkService();
            _appTracker = new ActiveAppTracker();

            _menuBarWindow = new MenuBarWindow(_config, _batteryService, _networkService, _appTracker, ShowSettings);
            _settingsWindow = new SettingsWindow(_config, _menuBarWindow);
            _trayController = new TrayController(_config, ToggleMenuBar, ShowSettings);

            _menuBarWindow.Loaded += (s, e) =>
            {
                var hwnd = new WindowInteropHelper(_menuBarWindow).Handle;
                _hotkeyManager = new GlobalHotkeyManager();
                _hotkeyManager.Register(hwnd, _config.Settings.GlobalShortcut);
                _hotkeyManager.HotkeyPressed += ToggleMenuBar;
            };

            if (_config.Settings.IsEnabled)
            {
                _menuBarWindow.Show();
            }
        }

        public void ToggleMenuBar()
        {
            if (_menuBarWindow == null || _config == null) return;

            if (_menuBarWindow.IsVisible)
            {
                _menuBarWindow.Hide();
                _config.Settings.IsEnabled = false;
                _trayController?.UpdateState(false);
            }
            else
            {
                _menuBarWindow.Show();
                _menuBarWindow.RepositionMenuBar();
                _config.Settings.IsEnabled = true;
                _trayController?.UpdateState(true);
            }
            _config.SaveSettings();
        }

        private void ShowSettings()
        {
            if (_settingsWindow != null)
            {
                _settingsWindow.Show();
                _settingsWindow.Activate();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayController?.Dispose();
            _hotkeyManager?.Dispose();
            _instanceMutex?.Dispose();
            base.OnExit(e);
        }
    }
}
