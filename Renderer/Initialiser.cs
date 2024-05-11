using Silk.NET.Direct3D12;
using System.Runtime.InteropServices;

namespace Renderer
{
    public class Initialiser
    {
        public async Task Run()
        {
            SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

            using (var debug = D3D12.GetApi().GetDebugInterface<ID3D12Debug3>())
            {
                debug.EnableDebugLayer();
                debug.SetEnableGPUBasedValidation(true);
                debug.SetEnableSynchronizedCommandQueueValidation(true);
                debug.SetGPUBasedValidationFlags(GpuBasedValidationFlags.None);
            }
        }

        [DllImport("user32.dll")]
        static extern uint SetProcessDpiAwarenessContext(IntPtr context);

        static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);
    }
}
