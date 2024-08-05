using Platform.Contracts;
using Renderer.Direct3D12;
using System.Runtime.InteropServices;

namespace Platform.Windows
{
    public class WindowsPlatform : IPlatform
    {
        public async Task<IRenderer> CreateRenderer(IWindow window, Options options)
        {
            if (window is Window w)
            {
                return new Direct3D12Renderer(await w.HWND, await w.GetSize(), options);
            }

            throw new InvalidOperationException("Window is not a Windows window");
        }

        public IWindow CreateWindow()
        {
            return new Window();
        }

        public Task OneTimeInitialisation()
        {
            SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

            return Task.CompletedTask;
        }

        static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        [DllImport("user32.dll")]
        static extern uint SetProcessDpiAwarenessContext(IntPtr context);
    }
}
