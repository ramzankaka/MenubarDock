using System;
using System.Windows.Interop;
using MenubarDock.Diagnostics;
using static MenubarDock.Core.NativeMethods;

namespace MenubarDock.Core
{
    public class GlobalHotkeyManager : IDisposable
    {
        private const int HOTKEY_ID = 9002;
        private IntPtr _hWnd;
        private HwndSource? _source;
        private bool _isRegistered;

        public event Action? HotkeyPressed;

        public bool Register(IntPtr hWnd, string shortcut = "Ctrl+Alt+M")
        {
            _hWnd = hWnd;
            Unregister();

            try
            {
                uint modifiers = MOD_CONTROL | MOD_ALT | MOD_NOREPEAT;
                uint vk = 0x4D; // 'M'

                _isRegistered = RegisterHotKey(_hWnd, HOTKEY_ID, modifiers, vk);
                if (_isRegistered)
                {
                    _source = HwndSource.FromHwnd(_hWnd);
                    _source?.AddHook(HwndHook);
                    Logger.Info($"Registered global hotkey {shortcut}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error registering hotkey", ex);
            }

            return _isRegistered;
        }

        public void Unregister()
        {
            if (_isRegistered && _hWnd != IntPtr.Zero)
            {
                try
                {
                    _source?.RemoveHook(HwndHook);
                    UnregisterHotKey(_hWnd, HOTKEY_ID);
                    _isRegistered = false;
                }
                catch { }
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            Unregister();
        }
    }
}
