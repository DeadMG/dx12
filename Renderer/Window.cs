using Data;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Renderer
{
    public interface IWindowListener
    {
        public void OnResize(ScreenSize size);
        public void OnMouseWheel(float amount, ScreenPosition pos);
        public void OnKeyDown(Key key);
        public void OnKeyUp(Key key);
        public void OnMouseDown(MouseButton button, ScreenPosition pos);
        public void OnMouseUp(MouseButton button, ScreenPosition pos);
        public void OnMouseMove(ScreenPosition pos);
    }

    public class Window
    {
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private readonly TaskCompletionSource<int> closedTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<IntPtr> hwndTcs = new TaskCompletionSource<IntPtr>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<ScreenSize> sizeTcs = new TaskCompletionSource<ScreenSize>(TaskCreationOptions.RunContinuationsAsynchronously);

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

        public IWindowListener? Listener { get; set; }

        private void WindowThread()
        {
            WndProc windowCallback = this.WindowCallback;

            var windowClass = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = windowCallback,
                lpszClassName = "WindowClass",
                style = CS_HREDRAW | CS_VREDRAW,
                hInstance = Marshal.GetHINSTANCE(GetType().Module),
                hCursor = LoadCursorW(IntPtr.Zero, new IntPtr(32512)) // IDC_ARROW
            };

            if (RegisterClassExW(ref windowClass) == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var hWnd = CreateWindowExW(0, windowClass.lpszClassName, "Dark Skies", WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, IntPtr.Zero, IntPtr.Zero, windowClass.hInstance, IntPtr.Zero);
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

                return 0;
            }
            if (msg == WM_SIZE)
            {
                if (GetClientRect(hWnd, out var rect))
                {
                    int width = rect.right - rect.left;
                    int height = rect.bottom - rect.top;

                    var newSize = new ScreenSize { Width = width, Height = height };

                    sizeTcs.TrySetResult(newSize);
                    Listener?.OnResize(newSize);

                    return 0;
                }
            }

            if (msg == WM_KEYDOWN)
            {
                if (keyboardKeys.TryGetValue(wParam, out var key))
                {
                    Listener?.OnKeyDown(key);
                    return 0;
                }
            }

            if (msg == WM_KEYUP)
            {
                if (keyboardKeys.TryGetValue(wParam, out var key))
                {
                    Listener?.OnKeyUp(key);
                    return 0;
                }
            }

            if (msg == WM_MOUSEWHEEL)
            {
                var rotation = unchecked((short)((wParam.ToInt64() >> 16) & 0xFFFF));
                var location = new POINT
                {
                    x = lParam.ToInt32() & 0xFFFF,
                    y = (lParam.ToInt32() >> 16) & 0xFFFF
                };

                var rotations = (float)rotation / WHEEL_DELTA;
                if (ScreenToClient(hWnd, ref location))
                {
                    Listener?.OnMouseWheel(rotations, new ScreenPosition { X = location.x, Y = location.y });
                }

                return 0;
            }

            if (msg == WM_MOUSEMOVE)
            {
                Listener?.OnMouseMove(PosFromLparam(lParam));

                return 0;
            }

            if (msg == WM_LBUTTONDOWN)
            {
                Listener?.OnMouseDown(MouseButton.Left, PosFromLparam(lParam));

                return 0;
            }

            if (msg == WM_LBUTTONUP)
            {
                Listener?.OnMouseUp(MouseButton.Left, PosFromLparam(lParam));

                return 0;
            }

            if (msg == WM_RBUTTONDOWN)
            {
                Listener?.OnMouseDown(MouseButton.Right, PosFromLparam(lParam));

                return 0;
            }

            if (msg == WM_RBUTTONUP)
            {
                Listener?.OnMouseUp(MouseButton.Right, PosFromLparam(lParam));

                return 0;
            }

            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private ScreenPosition PosFromLparam(IntPtr lParam)
        {
            var x = unchecked((short)(lParam.ToInt64() & 0xFFFF));
            var y = unchecked((short)((lParam.ToInt64() >> 16) & 0xFFFF));

            return new ScreenPosition { X = x, Y = y };
        }

        public Task Closed => closedTcs.Task;
        public Task<IntPtr> HWND => hwndTcs.Task;
        public Task<ScreenSize> InitialSize => sizeTcs.Task;

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
        const int WM_SIZE = 0x5;
        const int WM_MOUSEWHEEL = 0x20A;
        const int WM_KEYDOWN = 0x0100;
        const int WM_KEYUP = 0x101;
        const int WHEEL_DELTA = 120;
        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_LBUTTONUP = 0x0202;
        const int WM_MOUSEMOVE = 0x0200;
        const int WM_RBUTTONDOWN = 0x0204;
        const int WM_RBUTTONUP = 0x0205;

        struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll")]
        static extern IntPtr LoadCursorW(IntPtr hInstance, IntPtr cursorNameOrOrdinal);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetClientRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr CreateWindowExW(
            int dwExStyle,
            [MarshalAs(UnmanagedType.LPWStr)] string lpClassName,
            [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName,
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

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetMessageW(ref MSG lpMsg, IntPtr hWnd, uint min, uint max);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool TranslateMessage([In] ref MSG lpMsg);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr DispatchMessage([In] ref MSG lpmsg);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

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
            public int x;
            public int y;
        }

        private readonly Dictionary<IntPtr, Key> keyboardKeys = new Dictionary<IntPtr, Key>
        {
            { 0x57, Key.W },
            { 0x53, Key.S },
            { 0x44, Key.D },
            { 0x41, Key.A },
        };
    }
}
