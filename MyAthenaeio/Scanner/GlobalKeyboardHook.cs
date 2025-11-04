using System;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Diagnostics;

namespace MyAthenaeio.Scanner
{
    internal class GlobalKeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        private LowLevelKeyboardProc _proc;
        private IntPtr _hookId = IntPtr.Zero;

        public event EventHandler<Key> KeyPressed;

        public GlobalKeyboardHook()
        {
            _proc = HookCallback;
        }

        public void Hook()
        {
            if (_hookId != IntPtr.Zero)
                return; // Already hooked

            _hookId = SetHook(_proc);
        }

        public void Unhook()
        {
            if (_hookId == IntPtr.Zero)
                return; // Already unhooked

            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Key key = KeyInterop.KeyFromVirtualKey(vkCode);
                KeyPressed?.Invoke(this, key);
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            Unhook();
        }

        #region Native Methods

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll")]
        private static extern IntPtr UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        #endregion
    }
}
