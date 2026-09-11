using System;
using System.Diagnostics;
using Microsoft.Win32;
using MenubarDock.Diagnostics;

namespace MenubarDock.Core
{
    public static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "MenubarDock";

        public static bool IsStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool SetStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                if (key == null) return false;

                if (enable)
                {
                    string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(AppName, $"\"{exePath}\" --startup");
                        Logger.Info("Registered MenubarDock in Windows Startup.");
                        return true;
                    }
                }
                else
                {
                    key.DeleteValue(AppName, false);
                    Logger.Info("Removed MenubarDock from Windows Startup.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to update startup key.", ex);
            }

            return false;
        }
    }
}
