using Util;

namespace Renderer.Direct3D12
{
    internal class BackBuffers : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();

        public readonly List<Vortice.Direct3D12.ID3D12Resource> backBuffers = new List<Vortice.Direct3D12.ID3D12Resource>();

        public BackBuffers(Vortice.DXGI.IDXGISwapChain3 swapChain)
        {
            for (int i = 0; i < swapChain.Description1.BufferCount; ++i)
            {
                var backBuffer = disposeTracker.Track(swapChain.GetBuffer<Vortice.Direct3D12.ID3D12Resource>(i).Name($"Back buffer {i}"));

                backBuffers.Add(backBuffer);
            }
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
        }
    }
}
