using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Renderer
{
    public class Window
    {
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private readonly TaskCompletionSource<int> closedTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<IntPtr> hwndTcs = new TaskCompletionSource<IntPtr>(TaskCreationOptions.RunContinuationsAsynchronously);

        public Window()
        {
            new Thread(obj =>
            {
                try
                {
                    WindowThread();
                } 
                catch (Exception ex)
                {
                    closedTcs.TrySetException(ex);
                    hwndTcs.TrySetException(ex);
                }
            }) { IsBackground = true }.Start();
        }

        private void WindowThread()
        {
            WndProc windowCallback = this.WindowCallback;

            var windowClass = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = windowCallback,
                lpszClassName = "WindowClass",
                style = CS_HREDRAW | CS_VREDRAW,
                hInstance = Marshal.GetHINSTANCE(GetType().Module)
            };

            if (RegisterClassExW(ref windowClass) == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var hWnd = CreateWindowExW(0, windowClass.lpszClassName, "WindowName", WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, IntPtr.Zero, IntPtr.Zero, windowClass.hInstance, IntPtr.Zero);
            if (hWnd == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            else
            {
                hwndTcs.TrySetResult(hWnd);
            }
            ShowWindow(hWnd, SW_SHOWDEFAULT);

            var msg = new MSG();
            while (!cts.IsCancellationRequested && GetMessageW(ref msg, hWnd, 0, 0))
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }

            closedTcs.TrySetResult(0);
        }

        private IntPtr WindowCallback(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_DESTROY)
            {
                cts.Cancel();
            }
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        public Task Closed => closedTcs.Task;
        public Task<IntPtr> HWND => hwndTcs.Task;

        const int SW_SHOWDEFAULT = 10;
        const int CS_HREDRAW = 2;
        const int CS_VREDRAW = 1;
        const int WS_MAXIMIZEBOX = 0x00010000;
        const int WS_MINIMIZEBOX = 0x00020000;
        const int WS_THICKFRAME = 0x00040000;
        const int WS_SYSMENU = 0x00080000;
        const int WS_CAPTION = 0x00C00000;
        const int WS_OVERLAPPED = 0;
        const int WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
        const int CW_USEDEFAULT = int.MinValue;
        const int WM_DESTROY = 0x2;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr CreateWindowExW(
            int dwExStyle,
            [MarshalAs(UnmanagedType.LPTStr)] string lpClassName,
            [MarshalAs(UnmanagedType.LPTStr)] string lpWindowName,
            int dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern UInt16 RegisterClassExW(ref WNDCLASSEX lpWndClass);

        struct WNDCLASSEX
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public int style;
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public WndProc lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszMenuName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool GetMessageW(ref MSG lpMsg, IntPtr hWnd, uint min, uint max);

        [DllImport("user32.dll")]
        static extern bool TranslateMessage([In] ref MSG lpMsg);

        [DllImport("user32.dll")]
        static extern IntPtr DispatchMessage([In] ref MSG lpmsg);

        [DllImport("user32.dll")]
        static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        struct MSG
        {
            IntPtr hWnd;
            uint message;
            IntPtr wParam;
            IntPtr lParam;
            uint time;
            POINT pt;
        }

        struct POINT
        {
            int x;
            int y;
        }
    }
}
