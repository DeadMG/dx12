using SharpDX;
using SharpDX.Direct3D12;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Renderer
{
    public class Initialiser
    {
        public async Task Run()
        {
            SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

            var result = LoadLibraryW("C:\\Program Files\\Microsoft PIX\\2405.15.002-OneBranch_release\\WinPixGpuCapturer.dll");
            if (result == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            LoadLibraryW("C:\\Program Files\\Microsoft PIX\\2405.15.002-OneBranch_release\\WinPixTimingCapturer.dll");

            using (var debug = DebugInterface.Get())
            {
                debug.EnableDebugLayer();
            }

            Configuration.EnableObjectTracking = true;
        }

        [DllImport("user32.dll")]
        static extern uint SetProcessDpiAwarenessContext(IntPtr context);

        static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)]string fileName);
    }
}
