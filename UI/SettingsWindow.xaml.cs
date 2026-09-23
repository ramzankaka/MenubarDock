using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MenubarDock.Core;

namespace MenubarDock.UI
{
    public partial class SettingsWindow : Window
    {
        private readonly ConfigurationManager _config;
        private readonly MenuBarWindow _menuBarWindow;
        private bool _isInitializing = true;

        public SettingsWindow(ConfigurationManager config, MenuBarWindow menuBarWindow)
        {
            _config = config;
            _menuBarWindow = menuBarWindow;
            InitializeComponent();
            LoadValues();
            _isInitializing = false;
        }

        private void LoadValues()
        {
            var s = _config.Settings;

            // Theme
            foreach (ComboBoxItem item in CmbTheme.Items)
            {
                if (item.Tag?.ToString() == s.Theme)
                {
                    CmbTheme.SelectedItem = item;
                    break;
                }
            }
            if (CmbTheme.SelectedIndex < 0) CmbTheme.SelectedIndex = 0;

            SliderHeight.Value = s.BarHeight;
            SliderOpacity.Value = s.Opacity;

            ChkStartup.IsChecked = StartupManager.IsStartupEnabled();
            ChkAutoHide.IsChecked = s.AutoHideWhenAppsOpen;

            ChkAppleMenu.IsChecked = s.ShowAppleMenu;
            ChkActiveApp.IsChecked = s.ShowActiveApp;

            ChkClockWidget.IsChecked = s.ShowClockWidget;
            ChkShowDate.IsChecked = s.ClockShowDate;
            Chk24Hour.IsChecked = s.Clock24Hour;
            ChkShowSeconds.IsChecked = s.ClockShowSeconds;

            ChkBattery.IsChecked = s.ShowBatteryWidget;
            ChkWifi.IsChecked = s.ShowWifiWidget;
            ChkBluetooth.IsChecked = s.ShowBluetoothWidget;
            ChkVolume.IsChecked = s.ShowVolumeWidget;
            ChkFocus.IsChecked = s.ShowFocusWidget;
            ChkSearch.IsChecked = s.ShowSearchWidget;
            ChkControlCenter.IsChecked = s.ShowControlCenterWidget;
        }

        private void OnSettingChanged(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            SaveAndApply();
        }

        private void OnSettingChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            SaveAndApply();
        }

        private void SaveAndApply()
        {
            var s = _config.Settings;

            if (CmbTheme.SelectedItem is ComboBoxItem selectedTheme)
                s.Theme = selectedTheme.Tag?.ToString() ?? "FrostedGlass";

            s.BarHeight = SliderHeight.Value;
            s.Opacity = SliderOpacity.Value;

            StartupManager.SetStartup(ChkStartup.IsChecked ?? false);
            s.AutoHideWhenAppsOpen = ChkAutoHide.IsChecked ?? true;

            s.ShowAppleMenu = ChkAppleMenu.IsChecked ?? true;
            s.ShowActiveApp = ChkActiveApp.IsChecked ?? true;

            s.ShowClockWidget = ChkClockWidget.IsChecked ?? true;
            s.ClockShowDate = ChkShowDate.IsChecked ?? true;
            s.Clock24Hour = Chk24Hour.IsChecked ?? false;
            s.ClockShowSeconds = ChkShowSeconds.IsChecked ?? false;

            s.ShowBatteryWidget = ChkBattery.IsChecked ?? true;
            s.ShowWifiWidget = ChkWifi.IsChecked ?? true;
            s.ShowBluetoothWidget = ChkBluetooth.IsChecked ?? true;
            s.ShowVolumeWidget = ChkVolume.IsChecked ?? true;
            s.ShowFocusWidget = ChkFocus.IsChecked ?? true;
            s.ShowSearchWidget = ChkSearch.IsChecked ?? true;
            s.ShowControlCenterWidget = ChkControlCenter.IsChecked ?? true;

            _config.SaveSettings();
            _menuBarWindow.ApplyThemeStyles();
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e) => Hide();

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
