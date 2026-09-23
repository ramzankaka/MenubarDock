using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using static MenubarDock.Core.NativeMethods;

namespace MenubarDock.Services
{
    public static class SystemActions
    {
        // Navigation & System
        public static void OpenWindowsSearch() => SendWin(0x53);   // Win+S
        public static void OpenQuickSettings() => SendWin(0x41);   // Win+A
        public static void OpenNotifications() => SendWin(0x4E);   // Win+N
        public static void OpenClipboard() => SendWin(0x56);       // Win+V
        public static void ShowDesktop() => SendWin(0x44);         // Win+D
        public static void OpenTaskView() => SendWin(0x09);        // Win+Tab

        // Volume Controls
        public static void VolumeUp() => SendKey(VK_VOLUME_UP);
        public static void VolumeDown() => SendKey(VK_VOLUME_DOWN);
        public static void VolumeMute() => SendKey(VK_VOLUME_MUTE);

        // Window Management
        public static void MinimizeWindow(IntPtr targetHwnd = default)
        {
            ActivateTargetWindow(targetHwnd);
            SendWin(0x28);      // Win+Down
        }

        public static void MaximizeWindow(IntPtr targetHwnd = default)
        {
            ActivateTargetWindow(targetHwnd);
            SendWin(0x26);      // Win+Up
        }

        public static void SnapWindowLeft(IntPtr targetHwnd = default)
        {
            ActivateTargetWindow(targetHwnd);
            SendWin(0x25);      // Win+Left
        }

        public static void SnapWindowRight(IntPtr targetHwnd = default)
        {
            ActivateTargetWindow(targetHwnd);
            SendWin(0x27);      // Win+Right
        }

        public static void CloseActiveWindow(IntPtr targetHwnd = default)
        {
            ActivateTargetWindow(targetHwnd);
            SendAlt(0x73);      // Alt+F4
        }

        public static void HideOthers(IntPtr targetHwnd = default)
        {
            ActivateTargetWindow(targetHwnd);
            SendWin(0x24);      // Win+Home (minimizes all windows except foreground)
        }

        public static void ShowAll()
        {
            // Win + Shift + M (restore all minimized windows)
            keybd_event(VK_LWIN, 0, 0, UIntPtr.Zero);
            keybd_event(0x10, 0, 0, UIntPtr.Zero); // Shift
            keybd_event(0x4D, 0, 0, UIntPtr.Zero); // M
            keybd_event(0x4D, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(0x10, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        // Edit Actions
        public static void TriggerUndo(IntPtr targetHwnd = default)
        {
            ActivateTargetWindow(targetHwnd);
            SendCtrl(0x5A);     // Ctrl+Z
        }

        public static void TriggerRedo(IntPtr targetHwnd = default)
        {
            ActivateTargetWindow(targetHwnd);
            SendCtrl(0x59);     // Ctrl+Y
        }

        public static void TriggerCut(IntPtr targetHwnd = default)
        {
            ActivateTargetWindow(targetHwnd);
            SendCtrl(0x58);     // Ctrl+X
        }

        public static void TriggerCopy(IntPtr targetHwnd = default)
        {
            ActivateTargetWindow(targetHwnd);
            SendCtrl(0x43);     // Ctrl+C
        }

        public static void TriggerPaste(IntPtr targetHwnd = default)
        {
            ActivateTargetWindow(targetHwnd);
            SendCtrl(0x56);     // Ctrl+V
        }

        public static void TriggerSelectAll(IntPtr targetHwnd = default)
        {
            ActivateTargetWindow(targetHwnd);
            SendCtrl(0x41);     // Ctrl+A
        }

        // View Actions
        public static void TriggerFullscreen(IntPtr targetHwnd = default)
        {
            ActivateTargetWindow(targetHwnd);
            SendKey(0x7A);      // F11
        }

        public static void TriggerZoomIn(IntPtr targetHwnd = default)
        {
            ActivateTargetWindow(targetHwnd);
            SendCtrl(0xBB);     // Ctrl+=
        }

        public static void TriggerZoomOut(IntPtr targetHwnd = default)
        {
            ActivateTargetWindow(targetHwnd);
            SendCtrl(0xBD);     // Ctrl+-
        }

        public static void TriggerZoomReset(IntPtr targetHwnd = default)
        {
            ActivateTargetWindow(targetHwnd);
            SendCtrl(0x30);     // Ctrl+0
        }

        // Specific Settings Pages
        public static void OpenWifiSettings() => OpenUrl("ms-settings:network-wifi");
        public static void OpenNetworkSettings() => OpenUrl("ms-settings:network");
        public static void OpenAirplaneModeSettings() => OpenUrl("ms-settings:network-airplanemode");
        public static void OpenBluetoothSettings() => OpenUrl("ms-settings:bluetooth");
        public static void OpenSoundSettings() => OpenUrl("ms-settings:sound");
        public static void OpenVolumeMixer() => OpenUrl("ms-settings:apps-volume");
        public static void OpenPowerBatterySettings() => OpenUrl("ms-settings:powersleep");
        public static void OpenBatterySaverSettings() => OpenUrl("ms-settings:batterysaver");
        public static void OpenFocusSettings() => OpenUrl("ms-settings:quiethours");
        public static void OpenNotificationSettings() => OpenUrl("ms-settings:notifications");
        public static void OpenAboutThisPC() => OpenUrl("ms-settings:about");
        public static void OpenSystemSettings() => OpenUrl("ms-settings:");

        public static void OpenTaskManager()
        {
            try { Process.Start(new ProcessStartInfo { FileName = "taskmgr.exe", UseShellExecute = true }); } catch { }
        }

        public static void LockComputer()
        {
            try { LockWorkStation(); } catch { }
        }

        public static void SleepComputer()
        {
            try { SetSuspendState(false, true, true); } catch { }
        }

        public static void RestartComputer()
        {
            try { Process.Start(new ProcessStartInfo { FileName = "shutdown.exe", Arguments = "/r /t 0", UseShellExecute = true }); } catch { }
        }

        public static void ShutdownComputer()
        {
            try { Process.Start(new ProcessStartInfo { FileName = "shutdown.exe", Arguments = "/s /t 0", UseShellExecute = true }); } catch { }
        }

        public static void RestartComputerSafe()
        {
            var res = System.Windows.MessageBox.Show(
                "Are you sure you want to restart your PC?",
                "Restart Computer",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                RestartComputer();
            }
        }

        public static void ShutdownComputerSafe()
        {
            var res = System.Windows.MessageBox.Show(
                "Are you sure you want to shut down your PC?",
                "Shut Down Computer",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (res == MessageBoxResult.Yes)
            {
                ShutdownComputer();
            }
        }

        private static void ActivateTargetWindow(IntPtr targetHwnd)
        {
            if (targetHwnd != IntPtr.Zero && IsWindow(targetHwnd))
            {
                try
                {
                    SetForegroundWindow(targetHwnd);
                    Thread.Sleep(35);
                }
                catch { }
            }
        }

        private static void OpenUrl(string uri)
        {
            try { Process.Start(new ProcessStartInfo { FileName = uri, UseShellExecute = true }); } catch { }
        }

        private static void SendKey(byte vk)
        {
            keybd_event(vk, 0, 0, UIntPtr.Zero);
            keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        private static void SendWin(byte vk)
        {
            keybd_event(VK_LWIN, 0, 0, UIntPtr.Zero);
            keybd_event(vk, 0, 0, UIntPtr.Zero);
            keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        private static void SendCtrl(byte vk)
        {
            keybd_event(0x11, 0, 0, UIntPtr.Zero);
            keybd_event(vk, 0, 0, UIntPtr.Zero);
            keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(0x11, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        private static void SendAlt(byte vk)
        {
            keybd_event(0x12, 0, 0, UIntPtr.Zero);
            keybd_event(vk, 0, 0, UIntPtr.Zero);
            keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(0x12, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
    }
}
