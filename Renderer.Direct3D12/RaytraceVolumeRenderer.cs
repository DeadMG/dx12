using Util;

namespace Renderer.Direct3D12
{
    internal class RaytraceVolumeRenderer : IDisposable
    {
        private readonly DisposeTracker tracker = new DisposeTracker();

        public void Dispose()
        {
            tracker.Dispose();
        }
    }
}
