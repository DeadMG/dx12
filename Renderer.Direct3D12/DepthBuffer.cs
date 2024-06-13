using Data.Space;
using Util;

namespace Renderer.Direct3D12
{
    public class DepthBuffer : IDisposable
    {
        private readonly DisposeTracker tracker = new DisposeTracker();
        private readonly Vortice.Direct3D12.ID3D12Resource depthBuffer;
        private readonly Vortice.Direct3D12.CpuDescriptorHandle handle;

        public DepthBuffer(Vortice.Direct3D12.ID3D12Device device, ScreenSize size, Vortice.Direct3D12.CpuDescriptorHandle handle)
        {
            this.handle = handle;

            depthBuffer = tracker.Track(device.CreateCommittedResource(new Vortice.Direct3D12.HeapProperties(Vortice.Direct3D12.HeapType.Default),
               Vortice.Direct3D12.HeapFlags.None,
               new Vortice.Direct3D12.ResourceDescription(Vortice.Direct3D12.ResourceDimension.Texture2D, 0, (ulong)size.Width, size.Height, 1, 0, Vortice.DXGI.Format.D32_Float, 1, 0, Vortice.Direct3D12.TextureLayout.Unknown, Vortice.Direct3D12.ResourceFlags.AllowDepthStencil),
               Vortice.Direct3D12.ResourceStates.DepthWrite,
               new Vortice.Direct3D12.ClearValue { DepthStencil = new Vortice.Direct3D12.DepthStencilValue { Depth = 1.0f, Stencil = 0 }, Format = Vortice.DXGI.Format.D32_Float }));

            device.CreateDepthStencilView(depthBuffer, new Vortice.Direct3D12.DepthStencilViewDescription { Format = Vortice.DXGI.Format.D32_Float, ViewDimension = Vortice.Direct3D12.DepthStencilViewDimension.Texture2D, Flags = Vortice.Direct3D12.DepthStencilViewFlags.None }, handle);
        }

        public Vortice.Direct3D12.ID3D12Resource Resource => depthBuffer;
        public Vortice.Direct3D12.CpuDescriptorHandle Handle => handle;

        public void Dispose()
        {
            tracker.Dispose();
        }
    }
}
