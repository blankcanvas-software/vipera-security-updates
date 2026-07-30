using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ViperaSecurity.Services
{
    public class SystemTrayService
    {
        private const int WM_USER = 0x0400;
        public const int WM_TRAYICON = WM_USER + 0x0100;

        private const int NIM_ADD = 0x00000000;
        private const int NIM_MODIFY = 0x00000001;
        private const int NIM_DELETE = 0x00000002;

        private const int NIF_MESSAGE = 0x00000001;
        private const int NIF_ICON = 0x00000002;
        private const int NIF_TIP = 0x00000004;
        private const int NIF_INFO = 0x00000010;

        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_RBUTTONUP = 0x0205;

        private const int uID = 0x1000;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uID;
            public int uFlags;
            public int uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public int dwState;
            public int dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public int uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public int dwInfoFlags;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

        private IntPtr _hWnd;
        private IntPtr _hIcon;
        private bool _isCreated;

        public event Action? DoubleClicked;
        public event Action? RightClicked;

        public void Initialize(Window window)
        {
            var helper = new WindowInteropHelper(window);
            _hWnd = helper.Handle;

            var hwndSource = HwndSource.FromHwnd(_hWnd);
            hwndSource?.AddHook(WndProc);

            CreateTrayIcon();
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        private void CreateTrayIcon()
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
                {
                    _hIcon = ExtractIcon(IntPtr.Zero, exePath, 0);
                }

                if (_hIcon == IntPtr.Zero)
                {
                    _hIcon = LoadIcon(IntPtr.Zero, (IntPtr)32518); // IDI_SHIELD
                }

                var nid = new NOTIFYICONDATA
                {
                    cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                    hWnd = _hWnd,
                    uID = uID,
                    uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                    uCallbackMessage = WM_TRAYICON,
                    hIcon = _hIcon,
                    szTip = "Vipera Security - Real-Time Cyber Protection"
                };

                Shell_NotifyIcon(NIM_ADD, ref nid);
                _isCreated = true;
            }
            catch { }
        }

        public void ShowNotification(string title, string message)
        {
            try
            {
                var nid = new NOTIFYICONDATA
                {
                    cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                    hWnd = _hWnd,
                    uID = uID,
                    uFlags = NIF_INFO,
                    szInfo = message,
                    szInfoTitle = title,
                    dwInfoFlags = 1 // NIIF_INFO
                };
                Shell_NotifyIcon(NIM_MODIFY, ref nid);
            }
            catch { }
        }

        public void Remove()
        {
            if (!_isCreated) return;
            try
            {
                var nid = new NOTIFYICONDATA
                {
                    cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                    hWnd = _hWnd,
                    uID = uID
                };
                Shell_NotifyIcon(NIM_DELETE, ref nid);
                _isCreated = false;
            }
            catch { }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_TRAYICON)
            {
                int lp = lParam.ToInt32();
                if (lp == WM_LBUTTONDBLCLK)
                {
                    DoubleClicked?.Invoke();
                    handled = true;
                }
                else if (lp == WM_RBUTTONUP)
                {
                    RightClicked?.Invoke();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }
    }
}
