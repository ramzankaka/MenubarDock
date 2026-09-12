using System;

namespace MenubarDock.Models
{
    public class MenuBarSettings
    {
        public bool IsEnabled { get; set; } = true;
        public bool StartWithWindows { get; set; } = false;
        public string GlobalShortcut { get; set; } = "Ctrl+Alt+M";
        public double BarHeight { get; set; } = 30.0;
        public double Opacity { get; set; } = 0.88;
        // Themes: "DarkGlass" (Default / Same as TaskbarDock), "FrostedGlass", "LightGlass", "OLEDBlack"
        public string Theme { get; set; } = "DarkGlass";
        public bool AutoHideWhenAppsOpen { get; set; } = true;
        public bool ShowAppleMenu { get; set; } = true;
        public bool ShowActiveApp { get; set; } = true;
        public bool ShowFocusWidget { get; set; } = true;
        public bool ShowBluetoothWidget { get; set; } = true;
        public bool ShowWifiWidget { get; set; } = true;
        public bool ShowVolumeWidget { get; set; } = true;
        public bool ShowBatteryWidget { get; set; } = true;
        public bool ShowControlCenterWidget { get; set; } = true;
        public bool ShowSearchWidget { get; set; } = true;
        public bool ShowClockWidget { get; set; } = true;
        public bool Clock24Hour { get; set; } = false;
        public bool ClockShowSeconds { get; set; } = false;
        public bool ClockShowDate { get; set; } = true;
    }
}
