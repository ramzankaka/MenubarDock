using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using MenubarDock.Core;
using static MenubarDock.Core.NativeMethods;

namespace MenubarDock.Services
{
    public static class WindowDetectionService
    {
        private static bool _lastHasOpenWindows = false;
        private static DateTime _lastCheckTime = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMilliseconds(250);
        private static readonly uint _currentPid = (uint)Environment.ProcessId;

        private static readonly ConcurrentDictionary<uint, (string Name, DateTime CachedAt)> _procCache = new();
        private static readonly TimeSpan ProcCacheExpiry = TimeSpan.FromSeconds(30);

        private static readonly string[] IgnoredClasses = new[]
        {
            "Progman",
            "WorkerW",
            "Shell_TrayWnd",
            "Shell_SecondaryTrayWnd",
            "Windows.UI.Core.CoreWindow",
            "XamlExplorerHostIslandWindow",
            "MultitaskingViewFrame",
            "TaskListThumbnailWnd",
            "SideGrip",
            "TopLevelWindowForOverflowXamlIsland",
            "Windows.Internal.Shell.TabProxyWindow",
            "EdgeUiInputTopWndClass",
            "DummyDWMListenerWindow"
        };

        private static readonly string[] IgnoredProcesses = new[]
        {
            "shellexperiencehost",
            "searchhost",
            "startmenuexperiencehost",
            "lockapp",
            "taskbardock",
            "menubardock",
            "systemsettingsadminflows",
            "widgets",
            "textinputhost"
        };

        /// <summary>
        /// Checks whether any non-minimized, user-facing application window is open and visible on screen.
        /// </summary>
        public static bool HasOpenApplicationWindows(bool forceRefresh = false)
        {
            var now = DateTime.UtcNow;
            if (!forceRefresh && (now - _lastCheckTime) < CacheDuration)
            {
                return _lastHasOpenWindows;
            }

            bool foundWindow = false;

            EnumWindows((hWnd, lParam) =>
            {
                if (IsCandidateAppWindow(hWnd))
                {
                    foundWindow = true;
                    return false; // Stop enumeration early once an open window is found
                }
                return true; // Continue enumeration
            }, IntPtr.Zero);

            _lastHasOpenWindows = foundWindow;
            _lastCheckTime = now;
            return foundWindow;
        }

        /// <summary>
        /// Determines if a specific HWND is a real, visible, non-minimized user program window.
        /// </summary>
        public static bool IsCandidateAppWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return false;

            // 1. Must be visible
            if (!IsWindowVisible(hWnd)) return false;

            // 2. Must not be minimized (iconic)
            if (IsIconic(hWnd)) return false;

            // 3. ExStyle checks: ignore tool windows unless they are explicitly app windows
            int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            if ((exStyle & WS_EX_TOOLWINDOW) != 0 && (exStyle & WS_EX_APPWINDOW) == 0)
                return false;

            // 4. Must not have an owner window unless explicitly marked as app window
            IntPtr owner = GetWindow(hWnd, GW_OWNER);
            if (owner != IntPtr.Zero && (exStyle & WS_EX_APPWINDOW) == 0)
                return false;

            // 5. Must have a valid size on screen (not 0x0, collapsed, or off-screen phantom)
            if (!GetWindowRect(hWnd, out RECT rect) || rect.Width < 120 || rect.Height < 80)
                return false;

            // Exclude scratch or hidden windows parked at off-screen coordinates (e.g. -32000)
            if (rect.Right <= 0 || rect.Bottom <= 0 || rect.Left >= SystemParameters.VirtualScreenWidth || rect.Top >= SystemParameters.VirtualScreenHeight)
                return false;

            // 6. Check if cloaked by DWM (Windows 10/11 suspended/hidden UWP apps or other virtual desktops)
            try
            {
                if (DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0 && cloaked != 0)
                    return false;
            }
            catch { }

            // 7. Check class name
            var sbClass = new StringBuilder(256);
            GetClassName(hWnd, sbClass, sbClass.Capacity);
            string className = sbClass.ToString();

            // File Explorer is ALWAYS an application window!
            if (className.Equals("CabinetWClass", StringComparison.OrdinalIgnoreCase) ||
                className.Equals("ExploreWClass", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Exclude desktop shell, taskbars, and system overlays
            foreach (var ignored in IgnoredClasses)
            {
                if (className.Equals(ignored, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // 8. Window text check: user-facing application windows must have a non-empty title
            var sbTitle = new StringBuilder(256);
            GetWindowText(hWnd, sbTitle, sbTitle.Capacity);
            if (sbTitle.Length == 0) return false;

            // 9. Process verification: exclude our own process and background system processes
            GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid == 0 || pid == _currentPid) return false;

            string procName = GetCachedProcessName(pid);
            if (string.IsNullOrEmpty(procName)) return false;

            foreach (var ignored in IgnoredProcesses)
            {
                if (procName.Equals(ignored, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // If process is explorer.exe, only CabinetWClass/ExploreWClass count (handled above)
            if (procName.Equals("explorer", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private static string GetCachedProcessName(uint pid)
        {
            var now = DateTime.UtcNow;
            if (_procCache.TryGetValue(pid, out var cached) && (now - cached.CachedAt) < ProcCacheExpiry)
            {
                return cached.Name;
            }

            try
            {
                using var proc = Process.GetProcessById((int)pid);
                string name = proc.ProcessName.ToLowerInvariant();
                _procCache[pid] = (name, now);
                return name;
            }
            catch
            {
                _procCache[pid] = (string.Empty, now);
                return string.Empty;
            }
        }

        /// <summary>
        /// Determines whether the current foreground window is the desktop (wallpaper/icons).
        /// </summary>
        public static bool IsForegroundDesktop(out string className)
        {
            className = string.Empty;
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return true;

            var sbClass = new StringBuilder(256);
            GetClassName(hwnd, sbClass, sbClass.Capacity);
            className = sbClass.ToString();

            return className.Equals("Progman", StringComparison.OrdinalIgnoreCase) ||
                   className.Equals("WorkerW", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether the current foreground window is a File Explorer window.
        /// </summary>
        public static bool IsForegroundFileExplorer()
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return false;

            var sbClass = new StringBuilder(256);
            GetClassName(hwnd, sbClass, sbClass.Capacity);
            string className = sbClass.ToString();

            return className.Equals("CabinetWClass", StringComparison.OrdinalIgnoreCase) ||
                   className.Equals("ExploreWClass", StringComparison.OrdinalIgnoreCase);
        }
    }
}
